using System;
using System.IO;
using System.Diagnostics;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Linq;
using System.Collections.Generic;

namespace z3nCore.Utilities
{
    public class Snapper
    {
        private readonly IZennoPosterProjectModel _project;
        public Snapper(IZennoPosterProjectModel project)
        {
            _project = project;
        }
        public void SnapDir(string pathSnaps = null)
        {
            //var path = $"{project.Path}";
            if (string.IsNullOrEmpty( pathSnaps)) pathSnaps = _project.Var("snapsDir");
            var files = Directory.GetFiles(_project.Path, "*", SearchOption.TopDirectoryOnly);
            var syncConfig = Path.Combine(pathSnaps,".sync.txt");
            var accessConfig = Path.Combine(pathSnaps,".access.txt");

            List<string> accessList = new List<string>();
            List<string> projectsList = new List<string>();

            try{
	            projectsList = File.ReadAllLines(syncConfig).ToList();
            }
            catch (Exception ex)
            {
                _project.SendWarningToLog(ex.Message);
            }
            
            foreach (string file in files)
            {
                string destinationDir = Path.GetDirectoryName(file);
                string fileName = file.Replace(destinationDir + "\\", ""); 
	            string projectName = fileName.Split('.')[0];
	            string projectDir = Path.Combine(pathSnaps, projectName);
                string snapDir = projectDir + "\\snaphots";
                
                if (!Directory.Exists(snapDir))Directory.CreateDirectory(snapDir);

	            if (!projectsList.Contains(projectName + " : true") && !projectsList.Contains(projectName + " : false"))
		            projectsList.Add(projectName + " : false");
	            
	            if (projectsList.Contains(projectName + " : true"))
		            accessList.Add(projectName);	

                string fileHash = z3nCore.Api.Git.GetFileHash(file);
                bool hashExists = false;

                var snaps = Directory.GetFiles(snapDir, "*", SearchOption.TopDirectoryOnly);
                foreach (string snapFile in snaps)
                {
                    if (z3nCore.Api.Git.GetFileHash(snapFile) == fileHash)
                    {
                        hashExists = true;
                        break;
                    }
                }

                if (!hashExists)
                {
                    string copyPath = Path.Combine(projectDir, fileName);
                    File.Copy(file, copyPath, overwrite: true);
                    _project.log($"[UPDATED]: {copyPath}");


                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
                    string timestampedCopyPath = Path.Combine(snapDir, $"{timestamp}.{Path.GetFileNameWithoutExtension(fileName)}{Path.GetExtension(fileName)}");
                    File.Copy(file, timestampedCopyPath, overwrite: false);
                    _project.log($"[SNAP]: {timestampedCopyPath}");
                }
                else
                {
                    //project.SendInfoToLog($"[EXIST] {fileHash} {file} {snapDir}");
                }
            }

            if (!File.Exists(accessConfig)) File.WriteAllText(accessConfig,"");
            var currentAccess = File.ReadAllLines(accessConfig).ToList();

            foreach (string activeProject in accessList)
            {
                bool projectExistsInAccess = false;
                
                foreach (string accessLine in currentAccess)
                {
                    if (accessLine.Contains(activeProject))
                    {
                        projectExistsInAccess = true;
                        break;
                    }
                }
                if (!projectExistsInAccess)
                {
                    currentAccess.Add(activeProject);
                    _project.log($"[ACCESS] Added to access: {activeProject}");
                }
            }
            File.WriteAllLines(accessConfig, currentAccess);
            File.WriteAllLines(syncConfig, projectsList);
        }
        public void SnapCoreDll()
        {
            var fs = new FS(_project);

            #region Get Versions and Paths
            string currentProcessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string processDir = Path.GetDirectoryName(currentProcessPath);
            string path_ExternalAssemblies = Path.Combine(processDir, "ExternalAssemblies");
            string path_dll = Path.Combine(path_ExternalAssemblies, "z3nCore.dll");

            string DllVer = FileVersionInfo.GetVersionInfo(path_dll).FileVersion;
            string ZpVer = processDir.Split('\\')[5];
            _project.SendInfoToLog($"ZP: v{ZpVer}, z3nCore: v{DllVer}");
            _project.Var("vZP", ZpVer);
            _project.Var("vDll", DllVer);

            string path_z3nCoreRepo = @"w:\code_hard\.net\z3nCore\ExternalAssemblies\";
            string path_z3nFarmRepo = @"w:\work_hard\zenoposter\CURRENT_JOBS\.snaps\z3nFarm\";
            string path_snapsBase = @"w:\work_hard\zenoposter\CURRENT_JOBS\.snaps\";
            #endregion

            #region Copy Assemblies
            var sourceFiles = Directory.GetFiles(path_ExternalAssemblies, "*", SearchOption.TopDirectoryOnly);
            _project.log($"Copying {sourceFiles.Length} files from ExternalAssemblies");

            fs.CopyDir(path_ExternalAssemblies, path_z3nFarmRepo + @"ExternalAssemblies\");
            fs.CopyDir(path_ExternalAssemblies, path_z3nCoreRepo);
            #endregion

            #region Version Archive
            string versionPath = $@"w:\code_hard\.net\z3nCore\verions\v{DllVer}\z3nCore.dll";
            string versionDirectory = Path.GetDirectoryName(versionPath);

            if (!Directory.Exists(versionDirectory))
            {
                Directory.CreateDirectory(versionDirectory);
                _project.log($"Created: {versionDirectory}");
            }

            File.Copy(path_dll, versionPath, true);

            string depsFile = Path.Combine(versionDirectory, "dependencies.txt");
            var dllFiles = Directory.GetFiles(path_z3nCoreRepo, "*.dll", SearchOption.TopDirectoryOnly);
            using (var writer = new StreamWriter(depsFile, false))
            {
                foreach (var file in dllFiles)
                {
                    var info = FileVersionInfo.GetVersionInfo(file);
                    writer.WriteLine($"{Path.GetFileName(file)} : {info.FileVersion}");
                }
            }
            _project.log($"Archived v{DllVer} + {dllFiles.Length} deps");
            #endregion

            #region Update Projects
            var projectUpdates = new[]
            {
                new { SourceDir = "_z3nLnch", SourceFile = "_z3nLnch.zp", TargetFile = "z3nLauncher.zp" },
                new { SourceDir = "SAFU", SourceFile = "SAFU.zp", TargetFile = "SAFU.zp" },
                new { SourceDir = "DbBuilder", SourceFile = "DbBuilder.zp", TargetFile = "DbBuilder.zp" }
            };

            // Clean old files
            foreach (var proj in projectUpdates)
            {
                string targetPath = Path.Combine(path_z3nFarmRepo, proj.TargetFile);
                if (File.Exists(targetPath)) File.Delete(targetPath);
            }

            // Copy new versions
            int updated = 0, missing = 0;
            foreach (var proj in projectUpdates)
            {
                string sourcePath = Path.Combine(path_snapsBase, proj.SourceDir, proj.SourceFile);
                string targetPath = Path.Combine(path_z3nFarmRepo, proj.TargetFile);
                
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
            #endregion
        }

    }
}