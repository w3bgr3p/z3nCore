using System;
using System.Collections.Generic;
using System.Diagnostics;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.Utilities
{
    public class Sys_
    {
        
        public static void Killer(  IZennoPosterProjectModel project ,int memLim = 100, int tLim = 30, bool kill = true, bool showLog = true)
        {
            var _log = new Logger(project, showLog, "zKiller", true);
            
            var zIProcesses = new Dictionary<int, object[]>();
            string[] browserNames = new[] { "zbe1" }; 
            string[] processNames = new[] { "ZennoPoster" };
            var zProcesses = new Dictionary<int, object[]>();
            var ok = new List<string>();
            var heavy = new List<string>();
            var old = new List<string>();
            var toKill = new List<int>();
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
	            

	            if (mem > memLim )
		            heavy.Add($"{pid} \n    mem: {mem}mb age:{t}min");
	            if ( t > tLim)
	            {
		            old.Add($"{pid} \n    mem: {mem}mb age:{t}min");
		            toKill.Add(pid);
	            }
	            
	            if ( t < tLim && mem < memLim) 
		            ok.Add($"{pid} \n    mem: {mem}mb age:{t}min");
            }

            if(ok.Count!= 0)_log.Send($"ok ({ok.Count}): \n{string.Join("\n",ok)}");
            if(heavy.Count!= 0)_log.Send($"heavy ({heavy.Count}): \n{string.Join("\n",heavy)}",color:LogColor.Yellow);
            if(old.Count!= 0)_log.Send($"old ({old.Count}): \n{string.Join("\n",old)}",color:LogColor.Orange);


            if( kill && toKill.Count != 0)
            {
	            _log.Send($"killing {toKill.Count} processess");
	            foreach(int pid in toKill)
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
            }
            _log.Send($"main ({zennoMain.Count}): \n{string.Join("\n",zennoMain)}",color:LogColor.Gray);

        }
    }
    
        public class Sys
    {
        #region Public API

        /// <summary>
        /// Мониторит и при необходимости убивает процессы браузеров и ZennoPoster по критериям памяти и времени работы
        /// </summary>
        /// <param name="project">Проект для логирования</param>
        /// <param name="limMb">Лимит памяти в MB (процессы выше будут помечены как heavy)</param>
        /// <param name="limMin">Лимит времени работы в минутах (процессы старше будут убиты)</param>
        /// <param name="kill">Действительно убивать процессы (false = только мониторинг)</param>
        /// <param name="showLog">Показывать ли логи в UI</param>
        public static void Killer(
            IZennoPosterProjectModel project,
            int limMb = 100,
            int limMin = 30,
            bool kill = true,
            bool showLog = true)
        {
            var log = new Logger(project, showLog, "zKiller", true);

            #region Step 1: Monitor Browser Processes
            var browserProcesses = CollectProcesses(new[] { "zbe1" });
            log.Send($"Browser processes found: {browserProcesses.Count}");

            var analysis = AnalyzeProcesses(browserProcesses, limMb, limMin);
            
            LogProcessAnalysis(log, analysis);
            #endregion

            #region Step 2: Kill Old Browser Processes
            if (kill && analysis.ToKill.Count > 0)
            {
                log.Send($"Killing {analysis.ToKill.Count} old browser processes...");
                KillProcesses(log, analysis.ToKill);
            }
            else if (!kill && analysis.ToKill.Count > 0)
            {
                log.Send($"Would kill {analysis.ToKill.Count} processes (kill=false)", color:LogColor.Orange);
            }
            #endregion

            #region Step 3: Monitor Main ZennoPoster Process (after cleanup)
            // Пересобираем список ПОСЛЕ убийства браузеров, чтобы увидеть освобождение памяти
            var zennoProcesses = CollectProcesses(new[] { "ZennoPoster" });
            log.Send($"ZennoPoster main processes: {zennoProcesses.Count}");
            
            LogMainProcesses(log, zennoProcesses);
            #endregion
        }

        #endregion

        #region Process Collection

        private static List<ProcessInfo> CollectProcesses(string[] processNames)
        {
            var result = new List<ProcessInfo>();

            foreach (var processName in processNames)
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var proc in processes)
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
                        result.Add(info);
                    }
                    catch (Exception)
                    {
                        // Процесс мог завершиться между GetProcessesByName и обращением к свойствам
                        // Это нормально, просто пропускаем
                    }
                }
            }

            return result;
        }

        #endregion

        #region Process Analysis

        private static ProcessAnalysis AnalyzeProcesses(List<ProcessInfo> processes, int memLim, int timeLim)
        {
            var analysis = new ProcessAnalysis();

            foreach (var proc in processes)
            {
                var description = $"{proc.Pid} - mem: {proc.MemoryMB}MB, age: {proc.RuntimeMinutes}min";

                bool isHeavy = proc.MemoryMB > memLim;
                bool isOld = proc.RuntimeMinutes > timeLim;

                if (isOld)
                {
                    analysis.Old.Add(description);
                    analysis.ToKill.Add(proc.Pid);
                }
                else if (isHeavy)
                {
                    analysis.Heavy.Add(description);
                }
                else
                {
                    analysis.Ok.Add(description);
                }
            }

            return analysis;
        }

        #endregion

        #region Process Termination

        private static void KillProcesses(Logger log, List<int> pids)
        {
            foreach (int pid in pids)
            {
                try
                {
                    var proc = Process.GetProcessById(pid);
                    proc.Kill();
                    log.Send($"✓ Killed process {pid}");
                }
                catch (ArgumentException)
                {
                    // Процесс уже не существует - это ок
                    log.Send($"⊘ Process {pid} already terminated", color:LogColor.Gray);
                }
                catch (Exception ex)
                {
                    log.Warn($"✗ Failed to kill {pid}: {ex.Message}");
                }
            }
        }

        #endregion

        #region Logging

        private static void LogProcessAnalysis(Logger log, ProcessAnalysis analysis)
        {
            if (analysis.Ok.Count > 0)
            {
                log.Send($"✓ OK ({analysis.Ok.Count}):\n{string.Join("\n", analysis.Ok)}");
            }

            if (analysis.Heavy.Count > 0)
            {
                log.Send($"⚠ Heavy ({analysis.Heavy.Count}):\n{string.Join("\n", analysis.Heavy)}", 
                    color: LogColor.Yellow);
            }

            if (analysis.Old.Count > 0)
            {
                log.Send($"⏱ Old ({analysis.Old.Count}):\n{string.Join("\n", analysis.Old)}", 
                    color: LogColor.Orange);
            }
        }

        private static void LogMainProcesses(Logger log, List<ProcessInfo> processes)
        {
            if (processes.Count == 0) return;

            var lines = new List<string>();
            foreach (var proc in processes)
            {
                lines.Add($"{proc.Pid} - mem: {proc.MemoryMB}MB, age: {proc.RuntimeMinutes}min");
            }

            log.Send($"Main ZennoPoster ({processes.Count}):\n{string.Join("\n", lines)}", 
                color: LogColor.Gray);
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Информация о процессе для анализа
        /// </summary>
        private class ProcessInfo
        {
            public int Pid { get; set; }
            public string Name { get; set; }
            public long MemoryMB { get; set; }
            public int RuntimeMinutes { get; set; }
        }

        /// <summary>
        /// Результаты анализа процессов
        /// </summary>
        private class ProcessAnalysis
        {
            public List<string> Ok { get; set; } = new List<string>();
            public List<string> Heavy { get; set; } = new List<string>();
            public List<string> Old { get; set; } = new List<string>();
            public List<int> ToKill { get; set; } = new List<int>();
        }

        #endregion
    }
    
    
}