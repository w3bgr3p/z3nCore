using System;
using System.Collections.Generic;
using System.Diagnostics;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Linq;

namespace z3nCore.Utilities
{
    /// <summary>
    /// Системные утилиты для управления процессами ZennoPoster
    /// </summary>
    public class Sys_
    {
        /// <summary>
        /// Мониторит и убивает процессы браузеров и ZennoPoster по критериям памяти и времени работы
        /// </summary>
        /// <param name="project">Проект для логирования</param>
        /// <param name="memLim">Лимит памяти для браузеров в MB</param>
        /// <param name="tLim">Лимит времени работы браузеров в минутах</param>
        /// <param name="mainMemLim">Лимит памяти для главного процесса ZennoPoster в MB</param>
        /// <param name="killOld">Убивать старые браузеры (по времени)</param>
        /// <param name="killHeavy">Убивать тяжёлые браузеры (по памяти)</param>
        /// <param name="killMain">Убивать тяжёлый главный процесс (ОПАСНО: может убить себя!)</param>
        /// <param name="showLog">Показывать логи в UI</param>
        public static void Killer(
            IZennoPosterProjectModel project, int memLim = 1000, int tLim = 30, int mainMemLim = 20000, bool killOld = true, bool killHeavy = true, bool killMain = true, bool showLog = true)
        {
            var log = new Logger(project, showLog, "K!11", true);

            #region Step 1: Collect and Analyze Browser Processes
            
            var browserProcesses = new List<ProcessInfo>();
            var allProcs = Process.GetProcessesByName("zbe1");
            
            foreach (var proc in allProcs)
            {
                try
                {
                    var info = new ProcessInfo
                    {
                        Pid = proc.Id,
                        Name = proc.ProcessName,
                        MemoryMB = proc.WorkingSet64 / (1024 * 1024),
                        RuntimeMinutes = (int)(DateTime.Now - proc.StartTime).TotalMinutes
                    };
                    browserProcesses.Add(info);
                }
                catch
                {
                    // Процесс завершился - пропускаем
                }
            }

            log.Send($"Total browser processes: {browserProcesses.Count}");

            var ok = new List<string>();
            var heavy = new List<string>();
            var old = new List<string>();
            var killByTime = new List<int>();
            var killByMem = new List<int>();

            foreach (var proc in browserProcesses)
            {
                string desc = $"{proc.Pid} \n    mem: {proc.MemoryMB}mb age:{proc.RuntimeMinutes}min";
                
                bool isHeavy = proc.MemoryMB > memLim;
                bool isOld = proc.RuntimeMinutes > tLim;

                if (isHeavy)
                {
                    heavy.Add(desc);
                    killByMem.Add(proc.Pid);
                }
                
                if (isOld)
                {
                    old.Add(desc);
                    killByTime.Add(proc.Pid);
                }
                
                if (!isOld && !isHeavy)
                {
                    ok.Add(desc);
                }
            }

            if (ok.Count != 0)
                log.Send($"ok ({ok.Count}): \n{string.Join("\n", ok)}");
            
            if (heavy.Count != 0)
                log.Send($"heavy ({heavy.Count}): \n{string.Join("\n", heavy)}", color: LogColor.Yellow);
            
            if (old.Count != 0)
                log.Send($"old ({old.Count}): \n{string.Join("\n", old)}", color: LogColor.Orange);

            #endregion

            #region Step 2: Kill Old Browser Processes

            if (killOld && killByTime.Count != 0)
            {
                log.Send($"Killing {killByTime.Count} old processes...");
                foreach (int pid in killByTime)
                {
                    try
                    {
                        Process.GetProcessById(pid).Kill();
                        log.Send($"✓ Killed {pid}");
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex.Message);
                    }
                }
            }

            #endregion

            #region Step 3: Kill Heavy Browser Processes

            if (killHeavy && killByMem.Count != 0)
            {
                log.Send($"Killing {killByMem.Count} heavy processes...");
                foreach (int pid in killByMem)
                {
                    try
                    {
                        Process.GetProcessById(pid).Kill();
                        log.Send($"✓ Killed {pid}");
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex.Message);
                    }
                }
            }

            #endregion

            #region Step 4: Monitor Main ZennoPoster Processes (after browser cleanup)

            var zennoProcesses = new List<ProcessInfo>();
            var zennoProcs = Process.GetProcessesByName("ZennoPoster");

            foreach (var proc in zennoProcs)
            {
                try
                {
                    var info = new ProcessInfo
                    {
                        Pid = proc.Id,
                        Name = proc.ProcessName,
                        MemoryMB = proc.WorkingSet64 / (1024 * 1024),
                        RuntimeMinutes = (int)(DateTime.Now - proc.StartTime).TotalMinutes
                    };
                    zennoProcesses.Add(info);
                }
                catch
                {
                    // Процесс завершился - пропускаем
                }
            }

            var zennoMain = new List<string>();
            var killZP = new List<int>();

            foreach (var proc in zennoProcesses)
            {
                string desc = $"{proc.Pid} \n    mem: {proc.MemoryMB}mb age:{proc.RuntimeMinutes}min";
                zennoMain.Add(desc);

                if (proc.MemoryMB > mainMemLim)
                {
                    killZP.Add(proc.Pid);
                }
            }

            log.Send($"main ({zennoMain.Count}): \n{string.Join("\n", zennoMain)}", color: LogColor.Gray);

            #endregion

            #region Step 5: Kill Heavy Main Processes (DANGEROUS - may kill itself!)

            if (killMain && killZP.Count != 0)
            {
                log.Send($"⚠ SELF KILL WARNING: {killZP.Count} main processes", color: LogColor.Red);
                foreach (int pid in killZP)
                {
                    try
                    {
                        log.Send($"☠ Harakiri {pid}");
                        Process.GetProcessById(pid).Kill();
                        
                        // Если убили себя - этот код не выполнится
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex.Message);
                    }
                }
            }

            #endregion
        }

        #region Helper Classes

        private class ProcessInfo
        {
            public int Pid { get; set; }
            public string Name { get; set; }
            public long MemoryMB { get; set; }
            public int RuntimeMinutes { get; set; }
        }

        #endregion
    }
    
    public class Sys
    {
        /// <summary>
        /// Мониторит и убивает процессы браузеров и ZennoPoster по критериям памяти и времени работы
        /// НОВАЯ ВЕРСИЯ: с привязкой к аккаунтам через ProcAcc
        /// </summary>
        /// <param name="project">Проект для логирования</param>
        /// <param name="memLim">Лимит памяти для браузеров в MB</param>
        /// <param name="tLim">Лимит времени работы браузеров в минутах</param>
        /// <param name="mainMemLim">Лимит памяти для главного процесса ZennoPoster в MB</param>
        /// <param name="killOld">Убивать старые браузеры (по времени)</param>
        /// <param name="killHeavy">Убивать тяжёлые браузеры (по памяти)</param>
        /// <param name="killMain">Убивать тяжёлый главный процесс (ОПАСНО: может убить себя!)</param>
        /// <param name="showLog">Показывать логи в UI</param>
        public static void Killer(
            IZennoPosterProjectModel project, 
            int memLim = 1000, 
            int tLim = 30, 
            int mainMemLim = 20000, 
            bool killOld = true, 
            bool killHeavy = true, 
            bool killMain = true, 
            bool showLog = true)
        {
            var log = new Logger(project, showLog, "K!11", true);

            #region Step 1: Collect and Analyze Browser Processes (with Account info)
            
            var browserProcesses = new List<ProcInfo>();
            var pidToAcc = ProcAcc.GetAll(); // Получаем все связи PID → ACC
            
            Process[] allProcs = null;
            try
            {
                allProcs = Process.GetProcessesByName("zbe1");
                
                foreach (var proc in allProcs)
                {
                    try
                    {
                        var info = new ProcInfo
                        {
                            Pid = proc.Id,
                            Name = proc.ProcessName,
                            MemoryMB = proc.WorkingSet64 / (1024 * 1024),
                            RuntimeMinutes = (int)(DateTime.Now - proc.StartTime).TotalMinutes,
                            Acc = pidToAcc.ContainsKey(proc.Id) ? pidToAcc[proc.Id] : "unknown"
                        };
                        browserProcesses.Add(info);
                    }
                    catch { }
                }
            }
            finally
            {
                if (allProcs != null)
                {
                    foreach (var proc in allProcs)
                        proc?.Dispose();
                }
            }

            log.Send($"Total browser processes: {browserProcesses.Count}");

            var ok = new List<string>();
            var heavy = new List<string>();
            var old = new List<string>();
            var killByTime = new List<int>();
            var killByMem = new List<int>();

            foreach (var proc in browserProcesses)
            {
                string desc = $"PID:{proc.Pid} acc{proc.Acc} mem:{proc.MemoryMB}mb age:{proc.RuntimeMinutes}min";
                
                bool isHeavy = proc.MemoryMB > memLim;
                bool isOld = proc.RuntimeMinutes > tLim;

                if (isHeavy)
                {
                    heavy.Add(desc);
                    killByMem.Add(proc.Pid);
                }
                
                if (isOld)
                {
                    old.Add(desc);
                    killByTime.Add(proc.Pid);
                }
                
                if (!isOld && !isHeavy)
                {
                    ok.Add(desc);
                }
            }

            if (ok.Count != 0)
                log.Send($"ok ({ok.Count}): \n{string.Join("\n", ok)}");
            
            if (heavy.Count != 0)
                log.Send($"heavy ({heavy.Count}): \n{string.Join("\n", heavy)}", color: LogColor.Yellow);
            
            if (old.Count != 0)
                log.Send($"old ({old.Count}): \n{string.Join("\n", old)}", color: LogColor.Orange);

            #endregion

            #region Step 2: Kill Old Browser Processes

            if (killOld && killByTime.Count != 0)
            {
                log.Send($"Killing {killByTime.Count} old processes...");
                foreach (int pid in killByTime)
                {
                    try
                    {
                        using (var proc = Process.GetProcessById(pid))
                        {
                            proc.Kill();
                        }
                        log.Send($"✓ Killed {pid}");
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex.Message);
                    }
                }
                ProcAcc.ClearCache(); // Обновляем кеш после убийства
            }

            #endregion

            #region Step 3: Kill Heavy Browser Processes

            if (killHeavy && killByMem.Count != 0)
            {
                log.Send($"Killing {killByMem.Count} heavy processes...");
                foreach (int pid in killByMem)
                {
                    try
                    {
                        using (var proc = Process.GetProcessById(pid))
                        {
                            proc.Kill();
                        }
                        log.Send($"✓ Killed {pid}");
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex.Message);
                    }
                }
                ProcAcc.ClearCache(); // Обновляем кеш после убийства
            }

            #endregion

            #region Step 4: Monitor Main ZennoPoster Processes (after browser cleanup)

            var zennoProcesses = new List<ProcInfo>();
            Process[] zennoProcs = null;
            
            try
            {
                zennoProcs = Process.GetProcessesByName("ZennoPoster");

                foreach (var proc in zennoProcs)
                {
                    try
                    {
                        var info = new ProcInfo
                        {
                            Pid = proc.Id,
                            Name = proc.ProcessName,
                            MemoryMB = proc.WorkingSet64 / (1024 * 1024),
                            RuntimeMinutes = (int)(DateTime.Now - proc.StartTime).TotalMinutes,
                            Acc = "main"
                        };
                        zennoProcesses.Add(info);
                    }
                    catch { }
                }
            }
            finally
            {
                if (zennoProcs != null)
                {
                    foreach (var proc in zennoProcs)
                        proc?.Dispose();
                }
            }

            var zennoMain = new List<string>();
            var killZP = new List<int>();

            foreach (var proc in zennoProcesses)
            {
                string desc = $"PID:{proc.Pid} mem:{proc.MemoryMB}mb age:{proc.RuntimeMinutes}min";
                zennoMain.Add(desc);

                if (proc.MemoryMB > mainMemLim)
                {
                    killZP.Add(proc.Pid);
                }
            }

            log.Send($"main ({zennoMain.Count}): \n{string.Join("\n", zennoMain)}", color: LogColor.Gray);

            #endregion

            #region Step 5: Kill Heavy Main Processes (DANGEROUS - may kill itself!)

            if (killMain && killZP.Count != 0)
            {
                log.Send($"⚠ SELF KILL WARNING: {killZP.Count} main processes", color: LogColor.Red);
                foreach (int pid in killZP)
                {
                    try
                    {
                        log.Send($"☠ Harakiri {pid}");
                        using (var proc = Process.GetProcessById(pid))
                        {
                            proc.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex.Message);
                    }
                }
            }

            #endregion
        }

        #region Helper Classes

        private class ProcInfo
        {
            public int Pid { get; set; }
            public string Name { get; set; }
            public long MemoryMB { get; set; }
            public int RuntimeMinutes { get; set; }
            public string Acc { get; set; }
        }

        #endregion
        
        #region Advanced Account-Based Killers (NEW!)
        
        /// <summary>
        /// Убить все процессы конкретного аккаунта
        /// </summary>
        public static int KillAcc(IZennoPosterProjectModel project, string acc, bool showLog = true)
        {
            var log = new Logger(project, showLog, "K!11", true);
            
            int killed = ProcAcc.Kill(acc);
            log.Send($"Killed {killed} processes for acc{acc}");
            
            return killed;
        }
        
        /// <summary>
        /// Убить старые процессы аккаунта, оставить только самый новый
        /// </summary>
        public static int KillDuplicates(IZennoPosterProjectModel project, string acc, bool showLog = true)
        {
            var log = new Logger(project, showLog, "K!11", true);
            
            var pids = ProcAcc.GetPids(acc);
            if (pids.Count <= 1)
            {
                log.Send($"No duplicates for acc{acc}");
                return 0;
            }
            
            log.Warn($"Found {pids.Count} processes for acc{acc}");
            
            int killed = ProcAcc.KillOld(acc);
            log.Send($"Killed {killed} old duplicates, kept newest");
            
            return killed;
        }
        
        /// <summary>
        /// Убить самый жирный процесс аккаунта
        /// </summary>
        public static bool KillHeaviest(IZennoPosterProjectModel project, string acc, bool showLog = true)
        {
            var log = new Logger(project, showLog, "K!11", true);
            
            int heaviest = ProcAcc.GetHeaviest(acc);
            if (heaviest == 0)
            {
                log.Send($"No processes found for acc{acc}");
                return false;
            }
            
            using (var proc = Process.GetProcessById(heaviest))
            {
                var memMB = proc.WorkingSet64 / (1024 * 1024);
                log.Warn($"Killing heaviest PID:{heaviest} ({memMB}MB)");
                proc.Kill();
            }
            
            ProcAcc.ClearCache();
            return true;
        }
        
        /// <summary>
        /// Убить все дубликаты для всех аккаунтов
        /// </summary>
        public static int KillAllDuplicates(IZennoPosterProjectModel project, bool showLog = true)
        {
            var log = new Logger(project, showLog, "K!11", true);
            
            var all = ProcAcc.GetAll();
            var duplicates = all.GroupBy(x => x.Value)
                                .Where(g => g.Count() > 1)
                                .Select(g => g.Key);
            
            int totalKilled = 0;
            
            foreach (var acc in duplicates)
            {
                var pids = ProcAcc.GetPids(acc);
                log.Warn($"acc{acc}: {pids.Count} processes (duplicates!)");
                
                int killed = ProcAcc.KillOld(acc);
                totalKilled += killed;
                log.Send($"  Killed {killed} old processes");
            }
            
            log.Send($"Total killed: {totalKilled} duplicate processes", show: true);
            return totalKilled;
        }
        
        /// <summary>
        /// Мониторинг процессов с детальной статистикой по аккаунтам
        /// </summary>
        public static void Monitor(IZennoPosterProjectModel project, bool showLog = true)
        {
            var log = new Logger(project, showLog, "📊", true);
            
            var all = ProcAcc.GetAll();
            var grouped = all.GroupBy(x => x.Value);
            
            log.Send($"=== PROCESS MONITOR ===", show: true);
            log.Send($"Total processes: {all.Count}");
            log.Send($"Total accounts: {grouped.Count()}");
            
            foreach (var group in grouped.OrderBy(g => g.Key))
            {
                var acc = group.Key;
                var pids = group.Select(x => x.Key).ToList();
                
                if (pids.Count > 1)
                {
                    log.Warn($"\nacc{acc}: {pids.Count} processes (DUPLICATES!)");
                }
                else
                {
                    log.Send($"\nacc{acc}: {pids.Count} process");
                }
                
                var details = ProcAcc.GetDetails(acc);
                foreach (var d in details)
                {
                    log.Send($"  {d}", color: LogColor.Gray);
                }
            }
        }
        
        #endregion
    }
    
}

