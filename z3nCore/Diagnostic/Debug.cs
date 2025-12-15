using System;
using System.Linq;
using System.Collections.Generic;

using ZennoLab.CommandCenter;

using Newtonsoft.Json.Linq;


namespace z3nCore.Utilities
{
    public static class Diagnostic
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
    public static class VideoManager
    {
        private static readonly object _wmiLock = new object();
        public static string HWVideoVendor()
        {
            lock (_wmiLock)
            {
                try
                {
                    var searcher = new System.Management.ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                    var cards = searcher.Get().Cast<System.Management.ManagementObject>().ToList();
    
                    if (cards.Count > 1)
                    {
                        string name = (cards[1]["Name"]?.ToString() ?? "").ToLower();
        
                        if (name.Contains("nvidia")) return "NVIDIA";
                        if (name.Contains("amd")) return "AMD";
                        if (name.Contains("ati")) return "AMD";
                        if (name.Contains("intel")) return "Intel";
        
                        string originalName = cards[1]["Name"]?.ToString() ?? "";
                        return originalName.Split(' ').FirstOrDefault() ?? "";
                    }
                    else if (cards.Count > 0)
                    {
                        string name = (cards[0]["Name"]?.ToString() ?? "").ToLower();
        
                        if (name.Contains("nvidia")) return "NVIDIA";
                        if (name.Contains("amd")) return "AMD";
                        if (name.Contains("ati")) return "AMD";
                        if (name.Contains("intel")) return "Intel";
        
                        string originalName = cards[0]["Name"]?.ToString() ?? "";
                        return originalName.Split(' ').FirstOrDefault() ?? "";
                    }
    
                    return "";
                }
                catch
                {
                    return "";
                }
            }
            
            
        }
        public static string VideoVendor(this Instance instance)
        {
            string webglData = instance.WebGLPreferences.Save();
            var jObject = JObject.Parse(webglData);


            var vendor = jObject["parameters"]["default"]["UNMASKED_VENDOR"].ToString().ToLower();
            if (vendor.Contains("nvidia")) return "NVIDIA";
            if (vendor.Contains("amd")) return "AMD";
            if (vendor.Contains("ati")) return "AMD";
            if (vendor.Contains("intel")) return "Intel";
            return "";
        }
        public static bool ValidateVideoVendor(this Instance instance, bool thrw = false)
        {
            try
            {
                var HW = HWVideoVendor();
                var ins = instance.VideoVendor();

                if (HW != ins) return false;
                return true;
            }
            catch 
            {
               if(thrw) 
                   throw;
               return false;
            }

        }
    }

}
