using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;
using ZennoLab.InterfacesLibrary.Enums.Log;

namespace z3nCore.Api
{
    public class Git
    {
        #region Fields and Constants
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _log;
        private readonly string _token;
        private readonly string _username;
        private readonly string _organization;
        
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
        #endregion

        #region Constructors
        public Git(IZennoPosterProjectModel project, string token, string username, string organization = null)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _log = new Logger(_project, true);
            
            if (string.IsNullOrWhiteSpace(token) || !token.StartsWith("ghp_"))
                throw new ArgumentException("Invalid GitHub token. Must start with 'ghp_'", nameof(token));
                
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty", nameof(username));

            _token = token;
            _username = username;
            _organization = organization;
        }

        // Для обратной совместимости
        public Git(IZennoPosterProjectModel project) : this(project, "", "", null)
        {
            // Оставлен для совместимости, но потребует передачи данных в SyncRepositories
        }
        #endregion

        #region Public API
        public void SyncRepositories(string baseDir, string commitMessage = "ts")
        {
            #region Validation
            if (!Directory.Exists(baseDir))
            {
                _log.Send($"[!W]: Directory '{baseDir}' does not exist!");
                Thread.Sleep(5000);
                return;
            }

            // Если данные не были переданы в конструктор, требуем их здесь
            if (string.IsNullOrWhiteSpace(_token))
            {
                _log.Send("[!W]: GitHub token not provided in constructor!");
                Thread.Sleep(5000);
                return;
            }
            #endregion

            #region Load Projects Configuration
            var projectsList = LoadProjectsConfiguration(baseDir);
            if (projectsList.Count == 0)
            {
                _log.Send("[!W]: No projects found in .sync.txt or file is missing!");
                return;
            }
            #endregion

            #region Process Projects
            var stats = new SyncStatistics();
            stats.TotalFolders = projectsList.Count;
            
            _log.Send($"Processing base directory: {baseDir}");
            _log.Send($"Target: {GetGitHubPath()}");
            _log.Send($"Syncing {stats.TotalFolders} folders");

            foreach (string projectToSync in projectsList)
            {
                ProcessSingleProject(baseDir, projectToSync, commitMessage, stats);
            }
            #endregion

            #region Results Summary
            LogSummary(stats);
            #endregion
        }

        // Для обратной совместимости
        [Obsolete("Используйте конструктор с параметрами авторизации и SyncRepositories()")]
        public void Main(string baseDir, string token, string username, string commitMessage = "ts")
        {
            // Временный объект для совместимости
            var tempGit = new Git(_project, token, username);
            tempGit.SyncRepositories(baseDir, commitMessage);
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
        #endregion

        #region Private Implementation
        private List<string> LoadProjectsConfiguration(string baseDir)
        {
            var syncConfig = Path.Combine(baseDir, ".sync.txt");
            var projectsList = new List<string>();
            
            try
            {
                projectsList = File.ReadAllLines(syncConfig).ToList();
            }
            catch (Exception ex)
            {
                _project.SendWarningToLog(ex.Message);
            }
            
            return projectsList;
        }

        private void ProcessSingleProject(string baseDir, string projectToSync, string commitMessage, SyncStatistics stats)
        {
            try
            {
                #region Skip Check
                if (projectToSync.Contains("false"))
                {
                    _log.Send($"[SKIP] (sync is off for: {projectToSync})");
                    stats.FoldersSkipped++;
                    return;
                }
                #endregion

                #region Setup Project Path
                string subDir = Path.Combine(baseDir, projectToSync.Split(':')[0].Trim());
                string projectName = Path.GetFileName(subDir);
                string repoUrl = BuildRepositoryUrl(projectName);
                #endregion

                #region Size Validation
                var sizeCheck = CheckRepositorySize(subDir);
                if (!sizeCheck.IsValid)
                {
                    _log.Send($"Skipped {subDir}: {sizeCheck.Reason}");
                    stats.FoldersSkippedBySize++;
                    return;
                }
                #endregion

                #region Git Operations
                PerformGitOperations(subDir, repoUrl, commitMessage, sizeCheck, stats);
                #endregion
            }
            catch (Exception ex)
            {
                _log.Send($"[!W]: {ex.Message}");
                stats.ErrorCount++;
                Thread.Sleep(DELAY_AFTER_ERROR_MS);
            }
        }

        private void PerformGitOperations(string subDir, string repoUrl, string commitMessage, SizeCheckResult sizeCheck, SyncStatistics stats)
        {
            ConfigureSafeDirectory(subDir);
            
            if (!Directory.Exists(Path.Combine(subDir, ".git")))
            {
                RunGit("init", subDir);
                RunGit("checkout -b master", subDir);
            }
            
            RunGit($"config user.name \"{_username}\"", subDir);
            RunGit($"config user.email \"{_username}@users.noreply.github.com\"", subDir);

            if (!RunGit("remote -v", subDir).Contains("origin"))
            {
                RunGit($"remote add origin {repoUrl}", subDir);
            }
			else
			{
			    RunGit($"remote set-url origin {repoUrl}", subDir);
			}


            CreateOrUpdateGitignore(subDir);

            string status = RunGit("status --porcelain", subDir);
            if (string.IsNullOrWhiteSpace(status))
            {
                _log.Send($"[SKIP] (No changes): {subDir}");
                stats.FoldersSkipped++;
                return;
            }

            if (commitMessage == "ts") 
                commitMessage = DateTime.UtcNow.ToString("o");

            stats.FoldersWithChanges++;
            RunGit("add .", subDir);
            RunGit($"commit -m \"{commitMessage}\"", subDir);
			string currentRemote = RunGit("remote get-url origin", subDir);
			_log.Send($"[DEBUG] Current remote URL: {currentRemote}");
			_log.Send($"[DEBUG] Expected username: {_username}");
            RunGit("push origin master --force", subDir);

            string toLog = $"[COMMIT] {subDir} ({sizeCheck.TotalSizeMB:F1}MB, {sizeCheck.FilesCount} files)";
            _project.SendToLog(toLog, LogType.Info, true, LogColor.Blue);
            stats.SuccessfullyCommitted++;
            Thread.Sleep(DELAY_BETWEEN_REPOS_MS);
        }

        private string BuildRepositoryUrl(string projectName)
        {
            string owner = string.IsNullOrWhiteSpace(_organization) ? _username : _organization;
            return $"https://{_token}@github.com/{owner}/{projectName}.git";
        }

        private string GetGitHubPath()
        {
            return string.IsNullOrWhiteSpace(_organization) 
                ? $"github.com/{_username}" 
                : $"github.com/{_organization} (user: {_username})";
        }

        private void LogSummary(SyncStatistics stats)
        {
            _log.Send($"=======================Summary======================= \n" +
                     $"Total={stats.TotalFolders}, Changes={stats.FoldersWithChanges}, " +
                     $"Skipped={stats.FoldersSkipped}, SizeSkipped={stats.FoldersSkippedBySize}, " +
                     $"Committed={stats.SuccessfullyCommitted}, Failed={stats.ErrorCount}");
        }
        #endregion

        #region Helper Classes
        private class SyncStatistics
        {
            public int TotalFolders { get; set; }
            public int FoldersWithChanges { get; set; }
            public int FoldersSkipped { get; set; }
            public int SuccessfullyCommitted { get; set; }
            public int ErrorCount { get; set; }
            public int FoldersSkippedBySize { get; set; }
        }

        private class SizeCheckResult
        {
            public bool IsValid { get; set; }
            public string Reason { get; set; }
            public double TotalSizeMB { get; set; }
            public int FilesCount { get; set; }
        }
        #endregion

        #region Size and File Management
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
        #endregion

        #region Git Operations
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
        #endregion
        
    }
}