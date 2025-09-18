using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Security.Cryptography;
using System.Linq;
using ZennoLab.InterfacesLibrary.Enums.Log;
using System.Collections.Generic;

namespace z3nCore
{
    public class GHsync
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _log;
        
        private const long MAX_FILE_SIZE_MB = 100;
        private const long MAX_TOTAL_SIZE_MB = 1000;
        private const int DELAY_BETWEEN_REPOS_MS = 2000;
        private const int DELAY_AFTER_ERROR_MS = 5000;
        private const int MAX_FILES_COUNT = 10000;
        
        private readonly string[] EXCLUDED_EXTENSIONS = {
            ".exe", ".so", ".dylib", ".bin", ".obj", ".lib", ".a",
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2",
            ".iso", ".img", ".dmg", ".msi", ".deb", ".rpm",
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv",
            ".mp3", ".wav", ".flac", ".ogg", ".m4a",
            ".psd", ".ai", ".sketch", ".fig",
            ".db", ".sqlite", ".mdb", ".accdb"
        };
        
        public GHsync(IZennoPosterProjectModel project)
        {
            _project = project;
            _log = new Logger(_project, true);
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
            catch
            {
                return string.Empty;
            }
        }
        
        public void Main(string baseDir, string token, string username, string commmit = "ts")
        {
            

		    var syncConfig = Path.Combine(baseDir,".sync.txt");		
		    var projectsList = new List<string>();
		    
		    try{
			    projectsList = File.ReadAllLines(syncConfig).ToList();
		    }
		    catch (Exception ex)
		    {
			    _project.SendWarningToLog(ex.Message);
		    }
		    
		    
		    if (!Directory.Exists(baseDir))
            {
                _log.Send($"[!W]: Directory '{baseDir}' does not exist!");
                Thread.Sleep(5000);
                return;
            }

            if (string.IsNullOrWhiteSpace(token) || !token.StartsWith("ghp_"))
            {
                _log.Send("[!W]: Invalid GitHub token!");
                Thread.Sleep(5000);
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                _log.Send("[!W]: Username is required!");
                Thread.Sleep(5000);
                return;
            }
		    
		    
		    
		    
		    

            int totalFolders = 0, foldersWithChanges = 0, foldersSkipped = 0;
            int successfullyCommitted = 0, errorCount = 0, foldersSkippedBySize = 0;

            try
            {
                _log.Send($"Processing base directory: {baseDir}");
                totalFolders = projectsList.Count;
                _log.Send($"Syncing {totalFolders} folders");

                
			    
			    
			    
			    foreach (string projectToSync in projectsList)
                {

				    string subDir;
				    if (projectToSync.Contains("false"))
                    {
                        _log.Send($"[SKIP] (sync is off for: {projectToSync})");
                        foldersSkipped++;
                        continue;
                    }
				    else subDir = Path.Combine(baseDir, projectToSync.Split(':')[0].Trim());
				    
                    string projectName = Path.GetFileName(subDir);
                    string repoUrl = $"https://{token}@github.com/{username}/{projectName}.git";

                    try
                    {
                        var sizeCheck = CheckRepositorySize(subDir);
                        if (!sizeCheck.IsValid)
                        {
                            _log.Send($"Skipped {subDir}: {sizeCheck.Reason}");
                            foldersSkippedBySize++;
                            continue;
                        }

                        ConfigureSafeDirectory(subDir);

                        if (!Directory.Exists(Path.Combine(subDir, ".git")))
                        {
                            RunGit("init", subDir);
                            RunGit("checkout -b master", subDir);
                        }

                        if (!RunGit("remote -v", subDir).Contains("origin"))
                        {
                            RunGit($"remote add origin {repoUrl}", subDir);
                        }

                        CreateOrUpdateGitignore(subDir);

                        string status = RunGit("status --porcelain", subDir);
                        if (string.IsNullOrWhiteSpace(status))
                        {
                            _log.Send($"[SKIP] (No changes): {subDir}");
                            foldersSkipped++;
                            continue;
                        }
					    
					    if (commmit == "ts") commmit = DateTime.UtcNow.ToString("o");
									    
                        foldersWithChanges++;
                        RunGit("add .", subDir);
                        RunGit($"commit -m \"{commmit}\"", subDir);
                        RunGit("push origin master --force", subDir);

                        string toLog = $"[COMMIT] {subDir} ({sizeCheck.TotalSizeMB:F1}MB, {sizeCheck.FilesCount} files)";
                        
                        _project.SendToLog(toLog, LogType.Info, true, LogColor.Blue);
                        successfullyCommitted++;
					    Thread.Sleep(DELAY_BETWEEN_REPOS_MS);
                    }
                    catch (Exception ex)
                    {
                        _log.Send($"[!W]: {ex.Message}");
                        errorCount++;
                        Thread.Sleep(DELAY_AFTER_ERROR_MS);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Send($"Critical error: {ex.Message}");
                errorCount++;
            }

            _log.Send($"=======================Summary======================= \nTotal={totalFolders}, Changes={foldersWithChanges}, Skipped={foldersSkipped}, SizeSkipped={foldersSkippedBySize}, Committed={successfullyCommitted}, Failed={errorCount}");
        }
        
        private class SizeCheckResult
        {
            public bool IsValid { get; set; }
            public string Reason { get; set; }
            public double TotalSizeMB { get; set; }
            public int FilesCount { get; set; }
        }
        
        private SizeCheckResult CheckRepositorySize(string directory)
        {
            var result = new SizeCheckResult { IsValid = true };
            
            try
            {
                var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\.git\\")).ToArray();
                
                result.FilesCount = files.Length;
                
                if (result.FilesCount > MAX_FILES_COUNT)
                {
                    result.IsValid = false;
                    result.Reason = $"Too many files ({result.FilesCount} > {MAX_FILES_COUNT})";
                    return result;
                }
                
                double totalSizeMB = 0;
                foreach (string file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        double fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                        totalSizeMB += fileSizeMB;

                        string extension = Path.GetExtension(file).ToLower();
                        if (fileSizeMB > MAX_FILE_SIZE_MB || (EXCLUDED_EXTENSIONS.Contains(extension) && fileSizeMB > 1))
                        {
                            result.IsValid = false;
                            result.Reason = $"Contains large or binary files";
                            return result;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
                
                result.TotalSizeMB = totalSizeMB;
                if (totalSizeMB > MAX_TOTAL_SIZE_MB)
                {
                    result.IsValid = false;
                    result.Reason = $"Repository too large ({totalSizeMB:F1}MB > {MAX_TOTAL_SIZE_MB}MB)";
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Reason = $"Size check error: {ex.Message}";
            }
            
            return result;
        }
        
        private void CreateOrUpdateGitignore(string directory)
        {
            try
            {
                string gitignorePath = Path.Combine(directory, ".gitignore");
                var rules = new[] {
                    "*.exe", "*.so", "*.dylib", "*.bin", "*.obj", "*.lib", "*.a",
                    "*.zip", "*.rar", "*.7z", "*.tar", "*.gz", "*.bz2",
                    "*.mp4", "*.avi", "*.mkv", "*.mov", "*.mp3", "*.wav",
                    "*.db", "*.sqlite", "*.mdb",
                    ".vs/", ".vscode/", "*.user", "*.suo", "Thumbs.db", ".DS_Store",
                    "node_modules/", "packages/", "bin/", "obj/", "build/", "dist/"
                };

                if (!File.Exists(gitignorePath))
                {
                    File.WriteAllLines(gitignorePath, rules);
                }
                else
                {
                    var existing = File.ReadAllText(gitignorePath);
                    var missing = rules.Where(r => !existing.Contains(r)).ToArray();
                    if (missing.Any())
                    {
                        File.AppendAllLines(gitignorePath, missing);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Send($"Warning: Failed to update .gitignore in {directory}: {ex.Message}");
            }
        }

        private void ConfigureSafeDirectory(string directory)
        {
            try
            {
                string normalizedPath = Path.GetFullPath(directory).Replace('\\', '/');
                if (!RunGitGlobal("config --global --get-all safe.directory").Contains(normalizedPath))
                {
                    RunGitGlobal($"config --global --add safe.directory \"{normalizedPath}\"");
                }
            }
            catch (Exception ex)
            {
                _log.Send($"Warning: Failed to configure safe directory {directory}: {ex.Message}");
            }
        }

        private string RunGit(string args, string workingDir)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Git failed: {args}. Error: {error}");
                }

                return output;
            }
        }

        private string RunGitGlobal(string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Git global failed: {args}. Error: {error}");
                }

                return output;
            }
        }
    }
}