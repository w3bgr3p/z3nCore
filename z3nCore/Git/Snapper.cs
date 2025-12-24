using System;
using System.IO;
using System.Diagnostics;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Linq;
using System.Collections.Generic;

namespace z3nCore.Utilities
{
    /// <summary>
    /// Утилита для версионирования проектов ZennoPoster и управления сборками z3nCore.
    /// </summary>
    public class Snapper
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Db _db;
        public Snapper(IZennoPosterProjectModel project, string pgPass = null, string tableName = "!projects")
        {
            _project = project;
            _db = new Db(project, dbMode: "PostgreSQL", 
                pgHost: "localhost", pgPort: "5432", pgDbName: "postgres", pgUser: "postgres", pgPass: pgPass, 
                defaultTable: tableName
            );
        }
        
    /// <summary>
    /// Выполняет резервное копирование файлов текущего проекта на основе хеш-сумм.
    /// </summary>
    public void SnapDir(string pathProjects = null, string pathSnaps = null, string pathCs = null)
    {
        if (string.IsNullOrEmpty(pathSnaps)) 
            pathSnaps = _project.Var("snapsDir");
        if (string.IsNullOrEmpty(pathProjects)) 
            pathProjects = _project.Path;

        EnsureProjectsTable();
        
        var files = Directory.GetFiles(pathProjects, "*", SearchOption.TopDirectoryOnly);
        ProcessProjectFiles(files, pathSnaps, pathCs);
    }

    private void EnsureProjectsTable()
    {
        var tableStructure = new Dictionary<string, string>
        {
            { "id", "SERIAL PRIMARY KEY" },
            { "name", "TEXT" },
            { "creation", "TEXT" },
            { "last_upd", "TEXT" },
            { "dir_size", "TEXT" },
            { "files_count", "TEXT" },
            { "have_cs", "TEXT" },
            { "gh_synced", "TEXT" },
            { "public", "TEXT" },
            { "personal_access", "TEXT" }
        };
    
        _db.CreateTable(tableStructure, "!projects", log: false);
    }

    private void ProcessProjectFiles(string[] files, string pathSnaps, string pathCs)
    {
        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string projectName = fileName.Split('.')[0];
            string projectDir = Path.Combine(pathSnaps, projectName);
            string snapDir = Path.Combine(projectDir, "snaphots");

            if (!Directory.Exists(snapDir)) 
                Directory.CreateDirectory(snapDir);

            // Создать снапшот основного файла
            CreateSnapshotIfChanged(file, fileName, projectDir, snapDir);

            // Проверить CS файл
            bool hasCs = false;
            if (!string.IsNullOrEmpty(pathCs))
            {
                string csFileName = $"{projectName.ToLower()}.cs";
                string csFilePath = Path.Combine(pathCs, csFileName);
                hasCs = File.Exists(csFilePath);
                
                if (hasCs)
                {
                    CreateSnapshotIfChanged(csFilePath, csFileName, projectDir, snapDir);
                }
            }

            // Посчитать размер и файлы ПОСЛЕ создания снапшотов
            long dirSize = 0;
            int filesCount = 0;
            if (Directory.Exists(projectDir))
            {
                var allFiles = Directory.GetFiles(projectDir, "*", SearchOption.AllDirectories);
                dirSize = allFiles.Sum(f => new FileInfo(f).Length);
                filesCount = allFiles.Length;
            }

            // Даты создания/изменения директории
            string creationDate = Directory.GetCreationTime(projectDir).ToString("yyyy-MM-dd HH:mm:ss");
            string lastModified = Directory.GetLastWriteTime(projectDir).ToString("yyyy-MM-dd HH:mm:ss");

            // Создать/обновить запись в БД
            EnsureProjectInDb(projectName, hasCs, dirSize, filesCount, creationDate, lastModified);
        }
    }

    private void EnsureProjectInDb(string projectName, bool hasCs, long dirSize, int filesCount, string creationDate, string lastModified)
    {
        var exists = _db.Get("name", where: $"\"name\" = '{projectName}'");
        string sizeKb = (dirSize / 1024).ToString();
        
    
        _project.log($"[DB] {projectName}: hasCs={hasCs}, csFlag={hasCs}, size={sizeKb}KB, files={filesCount}, creation={creationDate}, modified={lastModified}");
    
        if (string.IsNullOrEmpty(exists))
        {
            var columns = "\"name\", \"creation\", \"last_upd\", \"dir_size\", \"files_count\", \"have_cs\", \"gh_synced\", \"public\", \"personal_access\"";
            var values = $"'{projectName}', '{creationDate}', '{lastModified}', '{sizeKb}', '{filesCount}', '{hasCs}', 'false', 'false', 'false'";
            _db.Query($"INSERT INTO \"!projects\" ({columns}) VALUES ({values})", log: true);
        }
        else
        {
            _db.Upd(
                $"creation = '{creationDate}', last_upd = '{lastModified}', dir_size = '{sizeKb}', files_count = '{filesCount}', have_cs = '{hasCs}'",
                where: $"\"name\" = '{projectName}'",
                log: true
            );
        }
    }

    public void UpdateProjectAccess(string projectName, bool ghSynced, bool isPublic, bool personalAccess)
    {
        var updates = new List<string>();
        
        if (ghSynced) updates.Add("gh_synced = 'true'");
        if (isPublic) updates.Add("public = 'true'");
        if (personalAccess) updates.Add("personal_access = 'true'");
        
        if (updates.Count > 0)
        {
            _db.Upd(
                string.Join(", ", updates), 
                "!projects", 
                log: true, 
                where: $"\"name\" = '{projectName}'"
            );
        }
    }

    private void CreateSnapshotIfChanged(string filePath, string fileName, 
        string projectDir, string snapDir)
    {
        string fileHash = z3nCore.Api.Git.GetFileHash(filePath);
        
        if (HashExistsInSnaps(snapDir, fileHash))
            return;

        // Update main copy
        string mainCopy = Path.Combine(projectDir, fileName);
        File.Copy(filePath, mainCopy, overwrite: true);
        _project.log($"[UPDATED]: {mainCopy}");

        // Create timestamped snapshot
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        string snapPath = Path.Combine(snapDir, $"{timestamp}.{nameWithoutExt}{extension}");
        File.Copy(filePath, snapPath, overwrite: false);
        _project.log($"[SNAP]: {snapPath}");
    }

    private bool HashExistsInSnaps(string snapDir, string targetHash)
    {
        var snaps = Directory.GetFiles(snapDir, "*", SearchOption.TopDirectoryOnly);
        foreach (string snapFile in snaps)
        {
            if (z3nCore.Api.Git.GetFileHash(snapFile) == targetHash)
                return true;
        }
        return false;
    }
        

        /// <summary>
        /// Архивирует текущую версию z3nCore.dll, фиксирует зависимости и обновляет рабочие проекты на "ферме".
        /// </summary>
        public void SnapCoreDll()
        {
            var fs = new FS(_project);
            var paths = GetCorePaths();
            var (dllVersion, zpVersion) = GetVersions(paths.DllPath, paths.ProcessDir);

            _project.SendInfoToLog($"ZP: v{zpVersion}, z3nCore: v{dllVersion}");
            _project.Var("vZP", zpVersion);
            _project.Var("vDll", dllVersion);

            CopyAssemblies(fs, paths);
            ArchiveVersion(paths, dllVersion);
            UpdateProjects(paths);
        }
        
        #region SnapCoreDll Methods

        private CorePaths GetCorePaths()
        {
            string currentProcessPath = Process.GetCurrentProcess().MainModule.FileName;
            string processDir = Path.GetDirectoryName(currentProcessPath);
            string externalAssemblies = Path.Combine(processDir, "ExternalAssemblies");

            return new CorePaths
            {
                ProcessDir = processDir,
                ExternalAssemblies = externalAssemblies,
                DllPath = Path.Combine(externalAssemblies, "z3nCore.dll"),
                Z3nCoreRepo = @"w:\code_hard\.net\z3nCore\ExternalAssemblies\",
                Z3nFarmRepo = @"w:\work_hard\zenoposter\CURRENT_JOBS\.snaps\z3nFarm\",
                SnapsBase = @"w:\work_hard\zenoposter\CURRENT_JOBS\.snaps\",
                VersionsBase = @"w:\code_hard\.net\z3nCore\verions\"
            };
        }

        private (string dllVersion, string zpVersion) GetVersions(string dllPath, string processDir)
        {
            string dllVersion = FileVersionInfo.GetVersionInfo(dllPath).FileVersion;
            string zpVersion = processDir.Split('\\')[5];
            return (dllVersion, zpVersion);
        }

        private void CopyAssemblies(FS fs, CorePaths paths)
        {
            var sourceFiles = Directory.GetFiles(paths.ExternalAssemblies, "*", SearchOption.TopDirectoryOnly);
            _project.log($"Copying {sourceFiles.Length} files from ExternalAssemblies");

            fs.CopyDir(paths.ExternalAssemblies, Path.Combine(paths.Z3nFarmRepo, "ExternalAssemblies"));
            fs.CopyDir(paths.ExternalAssemblies, paths.Z3nCoreRepo);
        }

        private void ArchiveVersion(CorePaths paths, string dllVersion)
        {
            string versionDir = Path.Combine(paths.VersionsBase, $"v{dllVersion}");
            string versionDll = Path.Combine(versionDir, "z3nCore.dll");

            if (!Directory.Exists(versionDir))
            {
                Directory.CreateDirectory(versionDir);
                _project.log($"Created: {versionDir}");
            }

            File.Copy(paths.DllPath, versionDll, true);

            // Create dependencies file
            string depsFile = Path.Combine(versionDir, "dependencies.txt");
            var dllFiles = Directory.GetFiles(paths.Z3nCoreRepo, "*.dll", SearchOption.TopDirectoryOnly);
            using (var writer = new StreamWriter(depsFile, false))
            {
                foreach (var file in dllFiles)
                {
                    var info = FileVersionInfo.GetVersionInfo(file);
                    writer.WriteLine($"{Path.GetFileName(file)} : {info.FileVersion}");
                }
            }
            _project.log($"Archived v{dllVersion} + {dllFiles.Length} deps");
        }

        private void UpdateProjects(CorePaths paths)
        {
            var projectUpdates = new[]
            {
                //new { SourceDir = "_z3nLnch", SourceFile = "_z3nLnch.zp", TargetFile = "z3nLauncher.zp" },
                new { SourceDir = "SAFU", SourceFile = "SAFU.zp", TargetFile = "SAFU.zp" },
                new { SourceDir = "DbBuilder", SourceFile = "DbBuilder.zp", TargetFile = "DbBuilder.zp" }
            };

            // Clean old files
            foreach (var proj in projectUpdates)
            {
                string targetPath = Path.Combine(paths.Z3nFarmRepo, proj.TargetFile);
                if (File.Exists(targetPath)) 
                    File.Delete(targetPath);
            }

            // Copy new versions
            int updated = 0, missing = 0;
            foreach (var proj in projectUpdates)
            {
                string sourcePath = Path.Combine(paths.SnapsBase, proj.SourceDir, proj.SourceFile);
                string targetPath = Path.Combine(paths.Z3nFarmRepo, proj.TargetFile);

                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, targetPath, true);
                    updated++;
                }
                else
                {
                    missing++;
                }
            }
            _project.log($"Projects: {updated} updated, {missing} missing");
        }

        #endregion

        private class CorePaths
        {
            public string ProcessDir { get; set; }
            public string ExternalAssemblies { get; set; }
            public string DllPath { get; set; }
            public string Z3nCoreRepo { get; set; }
            public string Z3nFarmRepo { get; set; }
            public string SnapsBase { get; set; }
            public string VersionsBase { get; set; }
        }
    }
}