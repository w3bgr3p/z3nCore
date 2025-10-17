
using System;
using System.Collections.Generic;


using System.Diagnostics;

using System.IO;
using System.Linq;

using System.Text.RegularExpressions;



namespace z3nCore.Utilities
{
/// <summary>
    /// Управление связями между процессами (PID) и аккаунтами (ACC)
    /// </summary>
        public static class ProcAcc
    {
        // Кеш: сканируем процессы только 1 раз
        private static Dictionary<int, string> _cache = null;
        private static DateTime _cacheTime = DateTime.MinValue;
        private static readonly object _cacheLock = new object();
        private static readonly int _cacheLifetimeMs = 2000; // 2 секунды
        
        // ============== ОСНОВНЫЕ МЕТОДЫ ==============
        
        /// <summary>
        /// Получить все связи PID → ACC (с кешированием)
        /// </summary>
        public static Dictionary<int, string> GetAll(bool forceRefresh = false)
        {
            lock (_cacheLock)
            {
                // Если кеш свежий и не требуется принудительное обновление
                if (!forceRefresh && _cache != null && 
                    (DateTime.Now - _cacheTime).TotalMilliseconds < _cacheLifetimeMs)
                {
                    return new Dictionary<int, string>(_cache);
                }
                
                // Обновляем кеш
                _cache = ScanAll();
                _cacheTime = DateTime.Now;
                
                return new Dictionary<int, string>(_cache);
            }
        }
        
        /// <summary>
        /// Получить все PID для аккаунта
        /// </summary>
        public static List<int> GetPids(string acc)
        {
            if (string.IsNullOrEmpty(acc)) return new List<int>();
            
            acc = Normalize(acc);
            var all = GetAll();
            
            return all.Where(x => Normalize(x.Value) == acc)
                      .Select(x => x.Key)
                      .ToList();
        }
        
        /// <summary>
        /// Получить ACC по PID
        /// </summary>
        public static string GetAcc(int pid)
        {
            var all = GetAll();
            return all.ContainsKey(pid) ? all[pid] : null;
        }
        
        /// <summary>
        /// Проверить, запущен ли аккаунт
        /// </summary>
        public static bool IsRunning(string acc)
        {
            return GetPids(acc).Count > 0;
        }
        
        /// <summary>
        /// Сбросить кеш (вызывать после Kill)
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _cache = null;
            }
        }
        
        // ============== ВЫБОР ПО КРИТЕРИЯМ ==============
        
        /// <summary>
        /// Получить самый новый (молодой) PID
        /// </summary>
        public static int GetNewest(string acc)
        {
            return GetBySelector(acc, (proc) => proc.StartTime, selectMax: true);
        }
        
        /// <summary>
        /// Получить самый старый PID
        /// </summary>
        public static int GetOldest(string acc)
        {
            return GetBySelector(acc, (proc) => proc.StartTime, selectMax: false);
        }
        
        /// <summary>
        /// Получить самый "жирный" (больше всего памяти) PID
        /// </summary>
        public static int GetHeaviest(string acc)
        {
            return GetBySelector(acc, (proc) => proc.WorkingSet64, selectMax: true);
        }
        
        /// <summary>
        /// Получить самый "легкий" (меньше всего памяти) PID
        /// </summary>
        public static int GetLightest(string acc)
        {
            return GetBySelector(acc, (proc) => proc.WorkingSet64, selectMax: false);
        }
        
        // ============== УПРАВЛЕНИЕ ПРОЦЕССАМИ ==============
        
        /// <summary>
        /// Завершить все процессы аккаунта
        /// </summary>
        public static int Kill(string acc)
        {
            int killed = 0;
            var pids = GetPids(acc);
            
            foreach (var pid in pids)
            {
                try
                {
                    using (var proc = Process.GetProcessById(pid))
                    {
                        proc.Kill();
                        killed++;
                    }
                }
                catch { }
            }
            
            if (killed > 0)
                ClearCache();
            
            return killed;
        }
        
        /// <summary>
        /// Завершить все процессы КРОМЕ самого нового
        /// </summary>
        public static int KillOld(string acc)
        {
            var allPids = GetPids(acc);
            int newestPid = GetNewest(acc);
            int killed = 0;
            
            foreach (var pid in allPids)
            {
                if (pid == newestPid) continue;
                
                try
                {
                    using (var proc = Process.GetProcessById(pid))
                    {
                        proc.Kill();
                        killed++;
                    }
                }
                catch { }
            }
            
            if (killed > 0)
                ClearCache();
            
            return killed;
        }
        
        /// <summary>
        /// Завершить конкретный PID
        /// </summary>
        public static bool KillPid(int pid)
        {
            try
            {
                using (var proc = Process.GetProcessById(pid))
                {
                    proc.Kill();
                    ClearCache();
                    return true;
                }
            }
            catch { return false; }
        }
        
        // ============== АНАЛИЗ ==============
        
        /// <summary>
        /// Получить детали всех процессов аккаунта
        /// </summary>
        public static List<string> GetDetails(string acc)
        {
            var result = new List<string>();
            var pids = GetPids(acc);
            
            foreach (var pid in pids)
            {
                try
                {
                    using (var proc = Process.GetProcessById(pid))
                    {
                        var memMB = proc.WorkingSet64 / (1024 * 1024);
                        var uptime = (DateTime.Now - proc.StartTime).TotalMinutes;
                        var threads = proc.Threads.Count;
                        
                        result.Add($"PID:{pid}, Mem:{memMB}MB, Up:{uptime:F0}min, Threads:{threads}");
                    }
                }
                catch { }
            }
            
            return result;
        }
        
        // ============== СЛУЖЕБНЫЕ МЕТОДЫ ==============
        
        /// <summary>
        /// ЕДИНСТВЕННОЕ место где происходит полное сканирование
        /// Все остальные методы используют результат этого метода
        /// </summary>
        private static Dictionary<int, string> ScanAll()
        {
            var result = new Dictionary<int, string>();
            var allPids = zbe1();
            
            foreach (var pid in allPids)
            {
                try
                {
                    var acc = GetAccFromPid(pid);
                    if (!string.IsNullOrEmpty(acc))
                        result[pid] = acc;
                }
                catch { }
            }
            
            return result;
        }
        public static List<int> zbe1()
        {
            var zProcesses = new List<int>();
            string[] processNames = new[] {  "zbe1" }; 
            var allProcs = new List<System.Diagnostics.Process>();
            foreach (var processName in processNames)
            {
                allProcs.AddRange(System.Diagnostics.Process.GetProcessesByName(processName));
            }

            if (allProcs.Count > 0)
            {
                foreach (var proc in allProcs)
                {
                    zProcesses.Add(proc.Id);
                    
                }
            }
            return zProcesses;
        }
        
        /// <summary>
        /// Универсальный селектор процесса по критерию
        /// </summary>
        private static int GetBySelector<T>(string acc, Func<Process, T> selector, bool selectMax) 
            where T : IComparable<T>
        {
            var pids = GetPids(acc);
            if (pids.Count == 0) return 0;
            if (pids.Count == 1) return pids[0];
            
            int selectedPid = 0;
            T selectedValue = default(T);
            bool first = true;
            
            foreach (var pid in pids)
            {
                try
                {
                    using (var proc = Process.GetProcessById(pid))
                    {
                        T value = selector(proc);
                        
                        if (first)
                        {
                            selectedPid = pid;
                            selectedValue = value;
                            first = false;
                        }
                        else
                        {
                            int comparison = value.CompareTo(selectedValue);
                            
                            if ((selectMax && comparison > 0) || (!selectMax && comparison < 0))
                            {
                                selectedPid = pid;
                                selectedValue = value;
                            }
                        }
                    }
                }
                catch { }
            }
            
            return selectedPid;
        }
        
        private static string GetAccFromPid(int pid)
        {
            using (var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + pid))
            using (var collection = searcher.Get())
            {
                foreach (System.Management.ManagementObject obj in collection)
                {
                    using (obj)
                    {
                        var cmdLineObj = obj["CommandLine"];
                        if (cmdLineObj == null) return null;
                        
                        string commandLine = cmdLineObj.ToString();
                        var match = Regex.Match(commandLine, @"--user-data-dir=""([^""]+)""");
                        
                        if (!match.Success || string.IsNullOrEmpty(match.Groups[1].Value))
                            return null;
                        
                        var path = match.Groups[1].Value.Trim('\\');
                        return Path.GetFileName(path);
                    }
                }
            }
            
            return null;
        }
        
        private static string Normalize(string acc)
        {
            if (string.IsNullOrEmpty(acc)) return string.Empty;
            return acc.Replace("acc", "").Replace("ACC", "").Trim();
        }
    }
}
    
