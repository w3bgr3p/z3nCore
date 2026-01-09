using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.Utilities
{
       public class AccountSocialData
   {
       public int AccountId { get; set; }
       public SocialStatus Twitter { get; set; }
       public SocialStatus GitHub { get; set; }
       public SocialStatus Discord { get; set; }
       public SocialStatus Telegram { get; set; }
        public AccountSocialData(int id)
       {
           AccountId = id;
       }
   }
   public class SocialStatus
   {
       public string Status { get; set; }  // "ok" или другое
       public string Login { get; set; }   // логин или username
        public bool IsActive => !string.IsNullOrEmpty(Login);
       public bool IsOk => Status == "ok";
   }
   
   public class ProjectData
   {
       public string ProjectName { get; set; }
       public Dictionary<string, string[]> All { get; set; }

       public static ProjectData CollectData(IZennoPosterProjectModel project, string tableName)
       {
           project.Var("projectTable", tableName.Trim());
           char _c = '¦';

           var allTouched = project.DbGetLines("id, last", where: "last like '+ %' OR last like '- %'");

           var All = new Dictionary<string, string[]>();

           foreach (var str in allTouched)
           {
               if (string.IsNullOrWhiteSpace(str)) continue;

               var columns = str.Split(_c);
               if (columns.Length < 2) continue;

               var acc = columns[0].Trim();
               var lastData = columns[1];

               if (string.IsNullOrWhiteSpace(lastData)) continue;

               var lines = lastData.Split('\n');
               if (lines.Length == 0) continue;

               var parts = lines[0].Split(' ');
               if (parts.Length < 2) continue;

               var completionStatus = parts[0].Trim();
               var ts = parts.Length >= 2 ? parts[1].Trim() : "";
               var completionSec = parts.Length >= 3 ? parts[2].Trim() : "";
               var report = lines.Length > 1 ? string.Join("\n", lines.Skip(1)).Trim() : "";

               if (!All.ContainsKey(acc))
               {
                   All.Add(acc, new [] { completionStatus, ts, completionSec, report });
               }
           }

           return new ProjectData
           {
               ProjectName = tableName.Replace("__", ""),
               All = All
           };
       }
   }
    
    
    
    public class JsonReportGenerator
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly bool _log;
        private const string TEMPLATES_REPO = "https://raw.githubusercontent.com/w3bgr3p/z3nCore/.templates";

        public JsonReportGenerator(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _log = log;
        }

        private void Log(string message) => _project.SendInfoToLog($"📊 [JsonReport] {message}", false);

        public void GenerateFullReport(string sortBy = "lastActivity")
        {
            Log("Запуск полного сбора данных...");

            // --- ЧАСТЬ 1: Сбор социальных аккаунтов ---
            var rangeStart = _project.Int("rangeStart");
            var rangeEnd = _project.Int("rangeEnd");
            if (rangeEnd < 100) rangeEnd = 100;

            var twitterData = ParseSocialData(_project, "_twitter", "id, status, login", rangeStart, rangeEnd);
            var githubData = ParseSocialData(_project, "_github", "id, status, login", rangeStart, rangeEnd);
            var discordData = ParseSocialData(_project, "_discord", "id, status, username", rangeStart, rangeEnd);
            var telegramData = ParseSocialData(_project, "_telegram", "id, username", rangeStart, rangeEnd);

            var socialAccounts = new List<AccountSocialData>();
            for (int i = rangeStart; i <= rangeEnd; i++)
            {
                var account = new AccountSocialData(i);
                if (twitterData.ContainsKey(i)) account.Twitter = new SocialStatus { Status = twitterData[i].GetValueOrDefault("status"), Login = twitterData[i].GetValueOrDefault("login") };
                if (githubData.ContainsKey(i)) account.GitHub = new SocialStatus { Status = githubData[i].GetValueOrDefault("status"), Login = githubData[i].GetValueOrDefault("login") };
                if (discordData.ContainsKey(i)) account.Discord = new SocialStatus { Status = discordData[i].GetValueOrDefault("status"), Login = discordData[i].GetValueOrDefault("username") };
                if (telegramData.ContainsKey(i)) account.Telegram = new SocialStatus { Status = "ok", Login = telegramData[i].GetValueOrDefault("username") };
                
                socialAccounts.Add(account);
            }

            // --- ЧАСТЬ 2: Сбор данных проектов (таблицы __) ---
            var projectTables = _project.TblList();
            var dailyProjects = new List<ProjectData>();
            foreach (var tbl in projectTables)
            {
                if (tbl.StartsWith("__") && !tbl.StartsWith("__|"))
                {
                    dailyProjects.Add(ProjectData.CollectData(_project, tbl));
                }
            }

            // --- ЧАСТЬ 3: Сортировка и Сохранение ---
            dailyProjects = ApplySorting(dailyProjects, sortBy);
            SaveAllToFiles(socialAccounts, dailyProjects);
        }

        private void SaveAllToFiles(List<AccountSocialData> socialAccounts, List<ProjectData> dailyProjects)
        {
            string dataFolder = Path.Combine(_project.Path, ".reports");
            string projectsFolder = Path.Combine(dataFolder, "projects");

            if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);
            if (!Directory.Exists(projectsFolder)) Directory.CreateDirectory(projectsFolder);

            // Сохраняем social.js
            var socialExport = new {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                accounts = socialAccounts.Select(acc => new {
                    id = acc.AccountId,
                    twitter = acc.Twitter?.IsActive == true ? new { status = acc.Twitter.Status, login = acc.Twitter.Login } : null,
                    github = acc.GitHub?.IsActive == true ? new { status = acc.GitHub.Status, login = acc.GitHub.Login } : null,
                    discord = acc.Discord?.IsActive == true ? new { status = acc.Discord.Status, login = acc.Discord.Login } : null,
                    telegram = acc.Telegram?.IsActive == true ? new { status = acc.Telegram.Status, login = acc.Telegram.Login } : null
                })
            };
            SaveAsJs(Path.Combine(dataFolder, "social.js"), "socialData", socialExport);

            // Сохраняем каждый проект
            foreach (var proj in dailyProjects)
            {
                SaveProjectFile(proj, projectsFolder);
            }

            // Ищем все существующие файлы process_*.js
            var machines = new List<string>();
            if (Directory.Exists(dataFolder))
            {
                var processFiles = Directory.GetFiles(dataFolder, "process_*.js");
                foreach (var file in processFiles)
                {
                    // Извлекаем имя машины из имени файла: process_MachineName.js -> MachineName
                    var fileName = Path.GetFileNameWithoutExtension(file); // process_MachineName
                    var machineName = fileName.Replace("process_", "");
                    machines.Add(machineName);
                }
                
                if (machines.Count > 0)
                {
                    Log($"📊 Найдено {machines.Count} машин(ы) с данными процессов: {string.Join(", ", machines)}");
                }
            }

            // Сохраняем metadata.js
            int maxIdx = dailyProjects.Any() ? dailyProjects.Max(p => p.All.Keys.Select(k => int.TryParse(k, out int i) ? i : 0).DefaultIfEmpty(0).Max()) : 0;
            
            var metadata = new {
                userId = _project.ExecuteMacro("{-Environment.CurrentUser-}"),
                generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                maxAccountIndex = maxIdx,
                projects = dailyProjects.Select(p => p.ProjectName).ToList(),
                machines = machines.Count > 0 ? machines : null  // Список машин из найденных файлов
            };
            SaveAsJs(Path.Combine(dataFolder, "metadata.js"), "reportMetadata", metadata);
            
            // Копируем шаблон HTML если его еще нет
            string htmlPath = Path.Combine(dataFolder, "unionReport.html");
            if (!File.Exists(htmlPath))
            {
                GenerateHtmlTemplate(htmlPath);
            }

            Log("✅ Все данные успешно собраны и сохранены в .js файлы.");
        }

        public void UpdateSingleProject(ProjectData projectData)
        {
            string projectsFolder = Path.Combine(_project.Path, ".reports", "projects");
            if (!Directory.Exists(projectsFolder)) Directory.CreateDirectory(projectsFolder);
            SaveProjectFile(projectData, projectsFolder);
        }

        private void SaveProjectFile(ProjectData project, string folder)
        {
            var data = new {
                name = project.ProjectName,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                accounts = project.All.ToDictionary(k => k.Key, v => new {
                    status = v.Value[0].Trim(),
                    timestamp = v.Value[1],
                    completionSec = v.Value[2].Trim(),
                    report = v.Value[3]
                })
            };
            SaveAsJs(Path.Combine(folder, $"{project.ProjectName}.js"), "project_" + CleanName(project.ProjectName), data);
        }

        private void SaveAsJs(string path, string varName, object data)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, $"window.{varName} = {json};", Encoding.UTF8);
        }

        private string CleanName(string name) => new string(name.Where(char.IsLetterOrDigit).ToArray());

        private static Dictionary<int, Dictionary<string, string>> ParseSocialData(IZennoPosterProjectModel project, string tableName, string columns, int start, int end)
        {
            var result = new Dictionary<int, Dictionary<string, string>>();
            project.Var("projectTable", tableName);
            var allLines = project.DbGetLines(columns, where: $"id >= {start} AND id <= {end}");
            var columnNames = columns.Split(',').Select(c => c.Trim()).ToArray();

            foreach (var line in allLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('¦');
                if (parts.Length < 2 || !int.TryParse(parts[0].Trim(), out int id)) continue;

                var data = new Dictionary<string, string>();
                for (int i = 1; i < columnNames.Length && i < parts.Length; i++)
                    data[columnNames[i]] = parts[i].Trim();
                result[id] = data;
            }
            return result;
        }

        private List<ProjectData> ApplySorting(List<ProjectData> projects, string sortBy)
        {
            switch (sortBy)
            {
                case "name": return projects.OrderBy(p => p.ProjectName).ToList();
                case "rate":
                    return projects.OrderByDescending(p => {
                        var success = p.All.Values.Count(v => v[0].Trim().StartsWith("+"));
                        return p.All.Count > 0 ? (double)success / p.All.Count : 0;
                    }).ToList();
                case "lastActivity":
                default:
                    return projects.OrderByDescending(p => {
                        DateTime latest = DateTime.MinValue;
                        foreach (var v in p.All.Values)
                            if (DateTime.TryParse(v[1], out DateTime ts) && ts > latest) latest = ts;
                        return latest;
                    }).ToList();
            }
        }
        
        public void GenerateProcessReport(string machineName = null)
        {
            if (string.IsNullOrEmpty(machineName)) 
             machineName = Environment.MachineName;
            var zennoProcesses = new List<object>();
            try
            {
                var zp = Diagnostic.ZennoProcesses(); 
                foreach (string[] arr in zp)
                {
                    int totalMinutes = int.Parse(arr[2]);
                    string uptime = $"{totalMinutes / 60}h {totalMinutes % 60}m";
                    
                    // Получаем командную строку процесса
                    string commandLine = "";
                    try
                    {
                        var processName = arr[0];
                        // Ищем процесс по имени и получаем командную строку
                        var processes = System.Diagnostics.Process.GetProcessesByName(processName.Replace(".exe", ""));
                        if (processes.Length > 0)
                        {
                            // Берем первый найденный процесс
                            var proc = processes[0];
                            try
                            {
                                // Пытаемся получить командную строку через WMI
                                using (var searcher = new System.Management.ManagementObjectSearcher(
                                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {proc.Id}"))
                                {
                                    foreach (System.Management.ManagementObject obj in searcher.Get())
                                    {
                                        commandLine = obj["CommandLine"]?.ToString() ?? "";
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                // Если не удалось получить через WMI - используем MainModule
                                commandLine = proc.MainModule?.FileName ?? "";
                            }
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки получения командной строки
                    }

                    zennoProcesses.Add(new {
                        name = arr[0],
                        ram = arr[1] + " MB",
                        uptime = uptime,
                        commandLine = commandLine
                    });
                }
            }
            catch (Exception ex)
            {
                Log("Ошибка Diagnostic.ZennoProcesses: " + ex.Message);
            }
            
            string safeMachineName = machineName.Replace("-", "_").Replace(" ", "_");

            var data = new {
                machineName = machineName,  // Оригинальное имя для отображения
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                processes = zennoProcesses
            };

            // Используем безопасное имя для файла и переменной
            string path = Path.Combine(_project.Path, ".reports", $"process_{safeMachineName}.js");
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, $"window.processData_{safeMachineName} = {json};", Encoding.UTF8);
            
            Log($"✅ Процессы сохранены: process_{safeMachineName}.js");
            
        }
        
        public void ImportProcessesFromJson(string jsonString, string machineName)
        {
            try
            {
                // Парсим JSON
                var processesData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonString);
                
                var zennoProcesses = new List<object>();
                foreach (var proc in processesData)
                {
                    // Ожидаем формат: [{"name": "...", "ram": "...", "uptime": "..."}]
                    zennoProcesses.Add(new {
                        name = proc.ContainsKey("name") ? proc["name"].ToString() : "Unknown",
                        ram = proc.ContainsKey("ram") ? proc["ram"].ToString() : "0 MB",
                        uptime = proc.ContainsKey("uptime") ? proc["uptime"].ToString() : "0h 0m"
                    });
                }

                var data = new {
                    machineName = machineName,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    processes = zennoProcesses
                };

                string path = Path.Combine(_project.Path, ".reports", $"process_{machineName}.js");
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, $"window.processData_{machineName} = {json};", Encoding.UTF8);
                
                Log($"✅ Процессы импортированы из JSON: process_{machineName}.js");
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка импорта процессов: {ex.Message}");
            }
        }
        
        private void GenerateHtmlTemplate(string path)
        {
            // Генерируем простой HTML шаблон который будет грузить все JS файлы
            var html = @"<!DOCTYPE html>
            <html lang='ru'>
            <head>
                <meta charset='UTF-8'>
                <title>Union Report</title>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    /* Здесь должны быть стили - скопируй их из unionReport.html который я дал */
                    body { background: #0d1117; color: #c9d1d9; font-family: monospace; }
                    .loading-overlay { 
                        position: fixed; top: 0; left: 0; right: 0; bottom: 0; 
                        background: rgba(13, 17, 23, 0.9); display: flex; 
                        align-items: center; justify-content: center; 
                        z-index: 9999; font-size: 18px; color: #58a6ff; 
                    }
                </style>
            </head>
            <body>
                <div id='loading' class='loading-overlay'>⏳ Loading data...</div>
                <div id='tooltip' class='tooltip'></div>
                <div class='container'>
                    <div class='header main-header'>
                        <h1 id='reportTitle'>📊 Union Report</h1>
                    </div>
                    <div id='processMonitor'></div>
                    <div class='section-header'><h2>🌐 Social Networks Status</h2></div>
                    <div class='summary-cards' id='socialSummary'></div>
                    <div class='section'><h2>Social Networks HeatMap</h2><div id='socialGrid'></div></div>
                    <div class='section-divider'></div>
                    <div class='section-header'><h2>📈 Daily Projects Status</h2></div>
                    <div class='summary-cards' id='dailySummary'></div>
                    <div class='section'><h2>Projects HeatMap</h2><div id='projectsGrid'></div></div>
                </div>
                <script src='metadata.js'></script>
                <script src='social.js'></script>
                <script>
                    if (window.reportMetadata && window.reportMetadata.projects) {
                        window.reportMetadata.projects.forEach(name => {
                            const s = document.createElement('script');
                            s.src = `projects/${name}.js`;
                            document.head.appendChild(s);
                        });
                    }
                    if (window.reportMetadata && window.reportMetadata.machines) {
                        window.reportMetadata.machines.forEach(m => {
                            const s = document.createElement('script');
                            s.src = `process_${m}.js`;
                            document.head.appendChild(s);
                        });
                    }
                </script>
                <script src='reportLoader.js'></script>
            </body>
            </html>";
            File.WriteAllText(path, html, Encoding.UTF8);
            Log($"HTML шаблон создан: {path}");
            Log("⚠️ ВАЖНО: Скопируй полные стили и reportLoader.js в папку .reports/");
        }
        
        private void EnsureTemplates()
        {
            string dataFolder = Path.Combine(_project.Path, ".reports");
            string htmlPath = Path.Combine(dataFolder, "unionReport.html");
            string jsPath = Path.Combine(dataFolder, "reportLoader.js");
        
            if (!File.Exists(htmlPath))
            {
                DownloadFile($"{TEMPLATES_REPO}/unionReport.html", htmlPath);
            }
        
            if (!File.Exists(jsPath))
            {
                DownloadFile($"{TEMPLATES_REPO}/reportLoader.js", jsPath);
            }
        }
         
        private void DownloadFile(string url, string savePath)
        {
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    client.DownloadFile(url, savePath);
                    Log($"✅ Скачан: {Path.GetFileName(savePath)}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка загрузки {url}: {ex.Message}");
                GenerateHtmlTemplate(savePath);
            }
        }
    }
}

// Хелпер для Dictionary
static class DictExt { 
    public static string GetValueOrDefault(this Dictionary<string, string> d, string k) => d.ContainsKey(k) ? d[k] : ""; 
}