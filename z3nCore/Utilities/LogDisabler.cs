using System;
using System.IO;
using System.Diagnostics;

namespace z3nCore
{
    public class LogDisabler
    {
        
        private static readonly object _disableLogsLock = new object();

        public static void DisableLogs(bool aggressive = false)
        {
            string currentProcessPath = Process.GetCurrentProcess().MainModule.FileName;
            string processDir = Path.GetDirectoryName(currentProcessPath);
            string pathLogs = Path.Combine(processDir, "Logs");
            
            lock (_disableLogsLock)
            {
                if (IsLogsAlreadyDisabled(pathLogs))
                {
                    return;
                }
                
                if (aggressive)
                {
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        if (IsLogsAlreadyDisabled(pathLogs))
                            return;
                            
                        TryDisableWithStrategy(pathLogs, true);
                        System.Threading.Thread.Sleep(50 * (attempt + 1));
                    }
                }
                else
                {
                    TryDisableWithStrategy(pathLogs, false);
                }
            }
        }

        private static bool IsLogsAlreadyDisabled(string pathLogs)
        {
            try
            {
                if (!Directory.Exists(pathLogs) && !File.Exists(pathLogs))
                {
                    return false;
                }
                
                if (File.Exists(pathLogs) && !Directory.Exists(pathLogs))
                {
                    return true;
                }
                
                if (Directory.Exists(pathLogs))
                {
                    var dirInfo = new DirectoryInfo(pathLogs);
                    if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        return true;
                    }
                }
                
                if (File.Exists(pathLogs + ".lock"))
                {
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static void TryDisableWithStrategy(string pathLogs, bool useAggressiveCommands)
        {
            try
            {
                if (IsLogsAlreadyDisabled(pathLogs))
                {
                    return;
                }
                
                if (useAggressiveCommands)
                {
                    if (Directory.Exists(pathLogs) && !IsSymbolicLink(pathLogs))
                    {
                        ExecuteCommand(string.Format("rd /s /q \"{0}\" 2>nul", pathLogs));
                    }
                    
                    if (!Directory.Exists(pathLogs))
                    {
                        ExecuteCommand(string.Format("mklink /d \"{0}\" \"NUL\" 2>nul", pathLogs));
                    }
                }
                else
                {
                    if (Directory.Exists(pathLogs) && !IsSymbolicLink(pathLogs))
                    {
                        foreach (string file in Directory.GetFiles(pathLogs, "*", SearchOption.AllDirectories))
                        {
                            try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
                        }
                        foreach (string dir in Directory.GetDirectories(pathLogs, "*", SearchOption.AllDirectories))
                        {
                            try { File.SetAttributes(dir, FileAttributes.Normal); } catch { }
                        }
                        Directory.Delete(pathLogs, true);
                    }
                    
                    if (!Directory.Exists(pathLogs) && !File.Exists(pathLogs))
                    {
                        ExecuteCommand(string.Format("mklink /d \"{0}\" \"NUL\"", pathLogs));
                    }
                }
            }
            catch 
            {
                if (!IsLogsAlreadyDisabled(pathLogs))
                {
                    try 
                    { 
                        if (Directory.Exists(pathLogs) && !IsSymbolicLink(pathLogs))
                        {
                            Directory.Delete(pathLogs, true);
                        }
                        
                        File.WriteAllText(pathLogs, "BLOCKED"); 
                    } 
                    catch { }
                    
                    try { File.SetAttributes(pathLogs, FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System); } catch { }
                    try { File.WriteAllText(pathLogs + ".lock", ""); } catch { }
                }
            }
        }

        private static bool IsSymbolicLink(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    return dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static void ExecuteCommand(string command)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c " + command;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                process.WaitForExit();
            }
        }
        
        
    }
}