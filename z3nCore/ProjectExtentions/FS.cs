using System;
using System.IO;
using System.Linq;
using ZennoLab.InterfacesLibrary.ProjectModel;


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
}
