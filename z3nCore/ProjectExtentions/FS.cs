using System;
using System.IO;
using System.Linq;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Collections.Generic;

namespace z3nCore
{
    public class FS
    {
        protected readonly IZennoPosterProjectModel _project;

        private readonly Logger _logger;
        private readonly object LockObject = new object();


        public FS(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "⚙️");

        }

        public void RmRf(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    DirectoryInfo dir = new DirectoryInfo(path);
                    dir.Attributes = FileAttributes.Normal; 

                    foreach (FileInfo file in dir.GetFiles())
                    {
                        file.IsReadOnly = false; 
                        file.Delete(); 
                    }

                    foreach (DirectoryInfo subDir in dir.GetDirectories())
                    {
                        RmRf(subDir.FullName); 
                    }
                    Directory.Delete(path, true);
                }
            }
            catch (Exception ex)
            {
                _logger.Send(ex.Message);
            }
        }
        
        public void CopyDir(string sourceDir, string destDir)
        {
            if (!Directory.Exists(sourceDir)) throw new DirectoryNotFoundException("Source directory does not exist: " + sourceDir);
            lock (LockObject)
            {
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                DirectoryInfo source = new DirectoryInfo(sourceDir);
                DirectoryInfo target = new DirectoryInfo(destDir);


                foreach (FileInfo file in source.GetFiles())
                {
                    string targetFilePath = Path.Combine(target.FullName, file.Name);
                    file.CopyTo(targetFilePath, true);
                }

                foreach (DirectoryInfo subDir in source.GetDirectories())
                {
                    string targetSubDirPath = Path.Combine(target.FullName, subDir.Name);
                    CopyDir(subDir.FullName, targetSubDirPath);
                }
            }
        }
        public static string GetRandomFile(string directoryPath)
        {
        readrandom:
            try
            {
                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                if (files.Length == 0) return null;
                var random = new Random();
                return files[random.Next(files.Length)];
            }
            catch 
            {
                goto readrandom;
            }
        }
        public  string GetNewCreds(string dataType)
        {
            string pathFresh = $"{_project.Path}.data\\fresh\\{dataType}.txt";
            string pathUsed = $"{_project.Path}.data\\used\\{dataType}.txt";

            lock (LockObject)
            {
                try
                {
                    if (!File.Exists(pathFresh))
                    {
                        _logger.Send($"File not found: {pathFresh}");
                        return null;
                    }

                    var freshAccs = File.ReadAllLines(pathFresh).ToList();
                    _logger.Send($"Loaded {freshAccs.Count} accounts from {pathFresh}");

                    if (freshAccs.Count == 0)
                    {
                        _logger.Send($"No accounts available in {pathFresh}");
                        return string.Empty;
                    }

                    string creds = freshAccs[0];
                    if (creds == "") throw new Exception($"noFreshDataLeft {dataType}");
                    freshAccs.RemoveAt(0);

                    File.WriteAllLines(pathFresh, freshAccs);
                    File.AppendAllText(pathUsed, creds + Environment.NewLine);

                    return creds;
                }
                catch (Exception ex)
                {
                    _logger.Send($"Error processing files for {dataType}: {ex.Message}");
                    return null;
                }
            }

        }
    }
    public static class FilePathHelper
    {
        /// <summary>
        /// Возвращает список всех путей файлов в указанной папке
        /// </summary>
        /// <param name="directoryPath">Путь к папке</param>
        /// <param name="includeSubdirectories">Включить подпапки (по умолчанию true)</param>
        /// <param name="searchPattern">Шаблон поиска файлов (по умолчанию "*" - все файлы)</param>
        /// <returns>Список путей к файлам</returns>
        public static List<string> GetAllFilePaths(string directoryPath, bool includeSubdirectories = true, string searchPattern = "*")
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Путь к папке не может быть пустым", "directoryPath");

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException("Папка не найдена: " + directoryPath);

            try
            {
                var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetFiles(directoryPath, searchPattern, searchOption).ToList();
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException("Нет доступа к папке: " + directoryPath, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при чтении папки " + directoryPath + ": " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Возвращает список относительных путей файлов в указанной папке
        /// </summary>
        /// <param name="directoryPath">Путь к папке</param>
        /// <param name="includeSubdirectories">Включить подпапки (по умолчанию true)</param>
        /// <param name="searchPattern">Шаблон поиска файлов (по умолчанию "*" - все файлы)</param>
        /// <returns>Список относительных путей к файлам</returns>
        public static List<string> GetAllRelativeFilePaths(string directoryPath, bool includeSubdirectories = true, string searchPattern = "*")
        {
            var fullPaths = GetAllFilePaths(directoryPath, includeSubdirectories, searchPattern);
            var directoryInfo = new DirectoryInfo(directoryPath);
            
            return fullPaths.Select(path => 
                GetRelativePath(directoryInfo.FullName, path)).ToList();
        }

        /// <summary>
        /// Вычисляет относительный путь (замена Path.GetRelativePath для .NET 4.6.2)
        /// </summary>
        /// <param name="fromPath">Базовый путь</param>
        /// <param name="toPath">Целевой путь</param>
        /// <returns>Относительный путь</returns>
        private static string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (string.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) 
                return toPath; // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Возвращает список путей файлов с определенными расширениями
        /// </summary>
        /// <param name="directoryPath">Путь к папке</param>
        /// <param name="extensions">Массив расширений файлов (например, new string[] {".cs", ".txt"})</param>
        /// <param name="includeSubdirectories">Включить подпапки (по умолчанию true)</param>
        /// <returns>Список путей к файлам с указанными расширениями</returns>
        public static List<string> GetFilePathsByExtensions(string directoryPath, string[] extensions, bool includeSubdirectories = true)
        {
            if (extensions == null || extensions.Length == 0)
                return GetAllFilePaths(directoryPath, includeSubdirectories);

            var allFiles = GetAllFilePaths(directoryPath, includeSubdirectories);
            
            return allFiles.Where(file => 
                extensions.Any(ext => 
                    file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).ToList();
        }
    }
    
        public static partial class ProjectExtensions
    {
        public static List<string> ListFromFile(this IZennoPosterProjectModel project, string listName, string fileName)
        {
            string web3prompts = $"{project.Path}.data\\web3prompts.txt";
            var prjList = project.Lists[listName];
            prjList.Clear();
            
            var lines = File.ReadAllLines(fileName).ToList();
            try
            {
                project.ListSync(listName, lines);
            }
            catch
            {
            }

            return lines;

        }
    }
}
