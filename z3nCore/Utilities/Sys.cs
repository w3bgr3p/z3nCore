using System;
using System.Collections.Generic;
using System.Diagnostics;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.Utilities
{
    public class Sys_
    {
        
        public static void Killer(  IZennoPosterProjectModel project ,int memLim = 100, int tLim = 30, int mainMemLim = 20000, bool killOld = true, bool killHeavy = true, bool killMain = true, bool showLog = true)
        {
            var _log = new Logger(project, showLog, "zKiller", true);
            
            var zIProcesses = new Dictionary<int, object[]>();
            string[] browserNames = new[] { "zbe1" }; 
            string[] processNames = new[] { "ZennoPoster" };
            var zProcesses = new Dictionary<int, object[]>();
            var ok = new List<string>();
            var heavy = new List<string>();
            var old = new List<string>();
            var killByTime = new List<int>();
            var killByMem = new List<int>();
            var killZP = new List<int>();
            var zennoMain = new List<string>();
            
            var allProcs = new List<System.Diagnostics.Process>();
            
            foreach (var processName in browserNames)
            {
                allProcs.AddRange(System.Diagnostics.Process.GetProcessesByName(processName));
            }
            if (allProcs.Count > 0)
            {
                foreach (var proc in allProcs)
                {
                    TimeSpan Time_diff = DateTime.Now - proc.StartTime;
                    string name = proc.ProcessName;
                    int runningTime = Convert.ToInt32(Time_diff.TotalMinutes);
                    long memoryUsage = (proc.WorkingSet64 / (1024 * 1024));
                    int pid = proc.Id;
                    zIProcesses.Add(pid, new object[]{name, memoryUsage, runningTime, pid});
                }
            }

            _log.Send($"total proc: {zIProcesses.Count}");
            foreach (var p in zIProcesses)
            {
	            int pid = p.Key;
	            long mem = (long)p.Value[1];
	            int t = (int)p.Value[2];


                if (mem > memLim)
                {
                    heavy.Add($"{pid} \n    mem: {mem}mb age:{t}min");
                    killByMem.Add(pid);
                }
	            if ( t > tLim)
	            {
		            old.Add($"{pid} \n    mem: {mem}mb age:{t}min");
		            killByTime.Add(pid);
	            }
	            
	            if ( t < tLim && mem < memLim) 
		            ok.Add($"{pid} \n    mem: {mem}mb age:{t}min");
            }

            if(ok.Count!= 0)_log.Send($"ok ({ok.Count}): \n{string.Join("\n",ok)}");
            if(heavy.Count!= 0)_log.Send($"heavy ({heavy.Count}): \n{string.Join("\n",heavy)}",color:LogColor.Yellow);
            if(old.Count!= 0)_log.Send($"old ({old.Count}): \n{string.Join("\n",old)}",color:LogColor.Orange);


            if( killOld && killByTime.Count != 0)
            {
	            _log.Send($"killing {killByTime.Count} processess");
	            foreach(int pid in killByTime)
	            {
		            try
		            {
			            Process.GetProcessById(pid).Kill(); 
			            _log.Send($"killed {pid}");
		            }
		            catch (Exception ex)
		            {
			            _log.Warn(ex.Message);
		            }
	            
	            }
            }
            if( killHeavy && killByMem.Count != 0)
            {
                _log.Send($"killing {killByTime.Count} processess");
                foreach(int pid in killByMem)
                {
                    try
                    {
                        Process.GetProcessById(pid).Kill(); 
                        _log.Send($"killed {pid}");
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex.Message);
                    }
	            
                }
            }

            allProcs = new List<System.Diagnostics.Process>();

            foreach (var processName in processNames)
            {
                allProcs.AddRange(System.Diagnostics.Process.GetProcessesByName(processName));
            }
            if (allProcs.Count > 0)
            {
                foreach (var proc in allProcs)
                {
                    TimeSpan Time_diff = DateTime.Now - proc.StartTime;
                    string name = proc.ProcessName;
                    int runningTime = Convert.ToInt32(Time_diff.TotalMinutes);
                    long memoryUsage = (proc.WorkingSet64 / (1024 * 1024));
                    int pid = proc.Id;
                    zProcesses.Add(pid, new object[]{name, memoryUsage, runningTime, pid});
                }
            }
            
            foreach (var p in zProcesses)
            {
	            int pid = p.Key;
	            long mem = (long)p.Value[1];
	            int t = (int)p.Value[2];
                zennoMain.Add($"{pid} \n    mem: {mem}mb age:{t}min");
                if (mem > mainMemLim)
                {
                    killZP.Add(pid);
                }
            }
            
            _log.Send($"main ({zennoMain.Count}): \n{string.Join("\n",zennoMain)}",color:LogColor.Gray);

            if( killMain && killZP.Count != 0)
            {
                _log.Send($"SELF KILL {killByTime.Count} ");
                foreach(int pid in killZP)
                {
                    try
                    {
                        Process.GetProcessById(pid).Kill(); 
                        _log.Send($"killed {pid}");
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex.Message);
                    }
	            
                }
            }
        }
    }
    



    /// <summary>
    /// Системные утилиты для управления процессами ZennoPoster
    /// </summary>
    public class Sys
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
}    
    
