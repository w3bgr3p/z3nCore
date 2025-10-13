using System;
using System.Linq;
using System.Collections.Generic;
namespace z3nCore.Utilities
{
    public static class Debugger
    {
        public static string AssemblyVer(string dllName)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == dllName);
            if (assembly != null)
            {
                return $"{dllName} {assembly.GetName().Version}, PublicKeyToken: {BitConverter.ToString(assembly.GetName().GetPublicKeyToken())}";
            }
            else
            {
                return $"{dllName} not loaded";
            }
        }
        public static List<string[]> ZennoProcesses()
        {
            var zProcesses = new List<string[]>();
            
            string[] processNames = new[] { "ZennoPoster", "zbe1" }; 
            
            var allProcs = new List<System.Diagnostics.Process>();
            foreach (var processName in processNames)
            {
                allProcs.AddRange(System.Diagnostics.Process.GetProcessesByName(processName));
            }

            if (allProcs.Count > 0)
            {
                foreach (var proc in allProcs)
                {
                    TimeSpan Time_diff = DateTime.Now - proc.StartTime;
                    int runningTime = Convert.ToInt32(Time_diff.TotalMinutes);
                    long memoryUsage = proc.WorkingSet64 / (1024 * 1024);
                    zProcesses.Add(new string[]{proc.ProcessName, memoryUsage.ToString(), runningTime.ToString()});
                }
                
            }
            return zProcesses;
        }
    }
}
