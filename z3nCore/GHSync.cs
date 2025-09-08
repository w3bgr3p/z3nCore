using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Security.Cryptography;

namespace z3nCore
{
    public class GHsync
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _log;
       
        
        public GHsync(IZennoPosterProjectModel project)
        {
            
            _project = project;
            _log = new Logger(_project,true);
        }
        
        public static string GetFileHash(string filePath)
        {
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
            catch (Exception ex)
            {
                //project.SendInfoToLog($"Hash err {filePath}: {ex.Message}");
                return string.Empty;
            }
        }

        
        
        public void Main(string baseDir, string token, string username)
        {
            if (!Directory.Exists(baseDir))
            {
                _log.Send($"Ошибка: Директория '{baseDir}' не существует!");
                Thread.Sleep(5000);
                return;
            }
    
            if (string.IsNullOrWhiteSpace(token) || !token.StartsWith("ghp_"))
            {
                _log.Send("Ошибка: Некорректный GitHub токен!");
                Thread.Sleep(5000);
                return;
            }
    
            if (string.IsNullOrWhiteSpace(username))
            {
                _log.Send("Ошибка: Имя пользователя не может быть пустым!");
                Thread.Sleep(5000);
                return;
            }
            
    
            int totalFolders = 0;
            int foldersWithChanges = 0;
            int foldersSkipped = 0;
            int successfullyCommitted = 0;
            int errorCount = 0;
    
            try
            {
                _log.Send("Starting GitHub sync process...");
                _log.Send($"Base directory: {baseDir}");
                _log.Send("");
    
                string[] subDirs = Directory.GetDirectories(baseDir);
                totalFolders = subDirs.Length;
    
                _log.Send($"Found {totalFolders} folders to process");
                _log.Send("Processing...");
                _log.Send("");
    
                foreach (string subDir in subDirs)
                {
                    string projectName = Path.GetFileName(subDir);
                    string repoUrl = $"https://{token}@github.com/{username}/{projectName}.git";
    
                    Console.Write($"Processing {projectName}... ");
    
                    try
                    {
                        ConfigureSafeDirectory(subDir);
    
                        if (!Directory.Exists(Path.Combine(subDir, ".git")))
                        {
                            RunGit("init", subDir);
                            RunGit("checkout -b master", subDir);
                        }
    
                        string remoteOutput = RunGit("remote -v", subDir);
                        if (!remoteOutput.Contains("origin"))
                        {
                            RunGit($"remote add origin {repoUrl}", subDir);
                        }
    
                        string status = RunGit("status --porcelain", subDir);
                        if (string.IsNullOrWhiteSpace(status))
                        {
                            _log.Send("SKIPPED (no changes)");
                            foldersSkipped++;
                            continue;
                        }
    
                        foldersWithChanges++;
    
                        RunGit("add .", subDir);
    
                        string timestamp = DateTime.UtcNow.ToString("o");
                        RunGit($"commit -m \"{timestamp}\"", subDir);
    
                        RunGit("push origin master --force", subDir);
                        
                        _log.Send("SUCCESS");
                        successfullyCommitted++;
                    }
                    catch (Exception ex)
                    {
                        _log.Send("ERROR");
                        
                        // Максимальное логирование при ошибках
                        _log.Send($"============ ERROR DETAILS FOR {projectName} ============");
                        _log.Send($"Directory: {subDir}");
                        _log.Send($"Repository URL: {repoUrl}");
                        _log.Send($"Error Type: {ex.GetType().Name}");
                        _log.Send($"Error Message: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            _log.Send($"Inner Exception: {ex.InnerException.Message}");
                        }
                        _log.Send($"Stack Trace: {ex.StackTrace}");
                        _log.Send($"========================================");
                        _log.Send("");
                        
                        errorCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                // Критические ошибки программы
                _log.Send($"CRITICAL ERROR: {ex.Message}");
                _log.Send($"Stack Trace: {ex.StackTrace}");
                errorCount++;
            }
    
            // Детальный итоговый отчет
            _log.Send("");
            _log.Send("==================== FINAL REPORT ====================");
            _log.Send($"Total folders found:           {totalFolders}");
            _log.Send($"Folders with changes:          {foldersWithChanges}");
            _log.Send($"Folders skipped (no changes):  {foldersSkipped}");
            _log.Send($"Successfully committed:        {successfullyCommitted}");
            _log.Send($"Errors occurred:               {errorCount}");
            _log.Send("======================================================");
            
            // Показываем статистику в процентах если есть папки
            if (totalFolders > 0)
            {
                _log.Send("");
                _log.Send("Statistics:");
                _log.Send($"Success rate: {(double)successfullyCommitted / totalFolders * 100:F1}%");
                _log.Send($"Skip rate:    {(double)foldersSkipped / totalFolders * 100:F1}%");
                _log.Send($"Error rate:   {(double)errorCount / totalFolders * 100:F1}%");
            }
    
            // Ожидание 10 секунд перед закрытием
            _log.Send("");
            _log.Send("Closing in 10 seconds...");
            Thread.Sleep(10000);
        }
    
        private void ConfigureSafeDirectory(string directory)
        {
            try
            {
                string normalizedPath = Path.GetFullPath(directory).Replace('\\', '/');
                
                string configOutput = RunGitGlobal($"config --global --get-all safe.directory");
                
                if (!configOutput.Contains(normalizedPath) && !configOutput.Contains(directory))
                {
                    RunGitGlobal($"config --global --add safe.directory \"{directory}\"");
                }
            }
            catch (Exception ex)
            {
                _log.Send("");
                _log.Send($"WARNING - Safe Directory Configuration Failed:");
                _log.Send($"Directory: {directory}");
                _log.Send($"Error: {ex.Message}");
                _log.Send("");
            }
        }
    
        private string RunGit(string args, string workingDir)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
    
            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
    
                if (process.ExitCode != 0)
                {
                    if (error.Contains("detected dubious ownership"))
                    {
                        throw new Exception($"Git detected dubious ownership in '{workingDir}'. Command: '{args}'. Please run as administrator or check file permissions. Full error: {error}");
                    }
                    
                    throw new Exception($"Git command failed in '{workingDir}'. Command: '{args}'. Exit code: {process.ExitCode}. Output: {output}. Error: {error}");
                }
    
                return output;
            }
        }
    
        private string RunGitGlobal(string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
    
            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
    
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Git global command failed. Command: '{args}'. Exit code: {process.ExitCode}. Output: {output}. Error: {error}");
                }
    
                return output;
            }
        }
    }
}