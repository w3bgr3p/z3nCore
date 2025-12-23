using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.Utilities
{
    public class JsonReportGenerator
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly bool _log;

        public JsonReportGenerator(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _log = log;
        }

        private void Log(string message)
        {
            if (_log)
            {
                _project.SendInfoToLog($"📊 [JsonReport] {message}", false);
            }
        }

        public void GenerateJsonData(List<AccountSocialData> socialAccounts, List<ProjectData> dailyProjects)
        {
            Log($"Generating JSON data: {socialAccounts.Count} social accounts + {dailyProjects.Count} projects...");

            string dataFolder = Path.Combine(_project.Path, ".reports");
            string projectsFolder = Path.Combine(dataFolder, "projects");

            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            if (!Directory.Exists(projectsFolder))
                Directory.CreateDirectory(projectsFolder);

            // 1. Generate social.json
            var socialJson = new
            {
                timestamp = DateTime.UtcNow,
                accounts = socialAccounts.Select(acc => new
                {
                    id = acc.AccountId,
                    twitter = acc.Twitter != null && acc.Twitter.IsActive ? new
                    {
                        status = acc.Twitter.Status,
                        login = acc.Twitter.Login
                    } : null,
                    github = acc.GitHub != null && acc.GitHub.IsActive ? new
                    {
                        status = acc.GitHub.Status,
                        login = acc.GitHub.Login
                    } : null,
                    discord = acc.Discord != null && acc.Discord.IsActive ? new
                    {
                        status = acc.Discord.Status,
                        login = acc.Discord.Login
                    } : null,
                    telegram = acc.Telegram != null && acc.Telegram.IsActive ? new
                    {
                        status = acc.Telegram.Status,
                        login = acc.Telegram.Login
                    } : null
                }).ToList()
            };

            string socialJsonPath = Path.Combine(dataFolder, "social.json");
            File.WriteAllText(socialJsonPath, JsonConvert.SerializeObject(socialJson, Formatting.Indented), Encoding.UTF8);
            Log($"Social data saved: {socialJsonPath}");

            // 2. Generate project JSON files
            foreach (var project in dailyProjects)
            {
                var projectJson = new
                {
                    name = project.ProjectName,
                    timestamp = DateTime.UtcNow,
                    accounts = project.All.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new
                        {
                            status = kvp.Value[0].Trim(),
                            timestamp = kvp.Value[1],
                            completionSec = kvp.Value[2].Trim(),
                            report = kvp.Value[3]
                        }
                    )
                };

                string projectJsonPath = Path.Combine(projectsFolder, $"{project.ProjectName}.json");
                File.WriteAllText(projectJsonPath, JsonConvert.SerializeObject(projectJson, Formatting.Indented), Encoding.UTF8);
            }

            Log($"Generated {dailyProjects.Count} project JSON files");

            // 3. Generate metadata.json
            int maxAccountIndex = 0;
            foreach (var project in dailyProjects)
            {
                foreach (var acc in project.All.Keys)
                {
                    if (int.TryParse(acc, out int accIndex))
                    {
                        if (accIndex > maxAccountIndex)
                            maxAccountIndex = accIndex;
                    }
                }
            }

            var metadata = new
            {
                userId = _project.ExecuteMacro("{-Environment.CurrentUser-}"),
                generatedAt = DateTime.UtcNow,
                maxAccountIndex = maxAccountIndex,
                projects = dailyProjects.Select(p => p.ProjectName).ToList()
            };

            string metadataPath = Path.Combine(dataFolder, "metadata.json");
            File.WriteAllText(metadataPath, JsonConvert.SerializeObject(metadata, Formatting.Indented), Encoding.UTF8);
            Log($"Metadata saved: {metadataPath}");
        }

        public void CopyStaticHtmlIfNeeded()
        {
            string dataFolder = Path.Combine(_project.Path, ".reports");

            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            string htmlPath = Path.Combine(dataFolder, "unionReport.html");
            string jsPath = Path.Combine(dataFolder, "reportLoader.js");

            if (!File.Exists(htmlPath))
            {
                _project.SendWarningToLog($"⚠️ Static HTML not found at: {htmlPath}", false);
                _project.SendWarningToLog($"⚠️ Please copy unionReport.html to .reports folder", false);
            }

            if (!File.Exists(jsPath))
            {
                _project.SendWarningToLog($"⚠️ Static JS not found at: {jsPath}", false);
                _project.SendWarningToLog($"⚠️ Please copy reportLoader.js to .reports folder", false);
            }
        }
    }
}

namespace z3nCore
{
    using Utilities;

    public static partial class ProjectExtensions
    {
        /// <summary>
        /// Генерирует JSON данные для отчёта (вместо HTML)
        /// </summary>
        public static void GenerateJsonReport(this IZennoPosterProjectModel project, 
            List<AccountSocialData> socialAccounts, 
            List<ProjectData> dailyProjects,
            bool log = false)
        {
            var generator = new JsonReportGenerator(project, log: log);
            generator.GenerateJsonData(socialAccounts, dailyProjects);
            generator.CopyStaticHtmlIfNeeded();
        }

        /// <summary>
        /// Полная генерация JSON отчёта (аналог GenerateFullHtmlReport)
        /// </summary>
        public static void GenerateFullJsonReport(this IZennoPosterProjectModel project, 
            string sortBy = "lastActivity", 
            bool openInBrowser = false,
            bool log = false)
        {
            if (log) project.SendInfoToLog("📊 Starting full JSON report generation...", false);

            var rangeStart = project.Int("rangeStart");
            var rangeEnd = project.Int("rangeEnd");
            if (rangeEnd < 100) rangeEnd = 100;

            // ===== ЧАСТЬ 1: Собираем Social данные =====
            if (log) project.SendInfoToLog($"📊 Fetching social data for accounts {rangeStart}-{rangeEnd}...", false);

            var twitterData = ParseSocialData(project, "_twitter", "id, status, login", rangeStart, rangeEnd);
            var githubData = ParseSocialData(project, "_github", "id, status, login", rangeStart, rangeEnd);
            var discordData = ParseSocialData(project, "_discord", "id, status, username", rangeStart, rangeEnd);
            var telegramData = ParseSocialData(project, "_telegram", "id, username", rangeStart, rangeEnd);

            var socialAccounts = new List<AccountSocialData>();

            for (int acc0 = rangeStart; acc0 <= rangeEnd; acc0++)
            {
                var account = new AccountSocialData(acc0);

                // Twitter
                if (twitterData.ContainsKey(acc0) && twitterData[acc0].ContainsKey("login"))
                {
                    account.Twitter = new SocialStatus
                    {
                        Status = twitterData[acc0].ContainsKey("status") ? twitterData[acc0]["status"] : "",
                        Login = twitterData[acc0].ContainsKey("login") ? twitterData[acc0]["login"] : ""
                    };
                }

                // GitHub
                if (githubData.ContainsKey(acc0) && githubData[acc0].ContainsKey("login"))
                {
                    account.GitHub = new SocialStatus
                    {
                        Status = githubData[acc0].ContainsKey("status") ? githubData[acc0]["status"] : "",
                        Login = githubData[acc0].ContainsKey("login") ? githubData[acc0]["login"] : ""
                    };
                }

                // Discord
                if (discordData.ContainsKey(acc0) && discordData[acc0].ContainsKey("username"))
                {
                    account.Discord = new SocialStatus
                    {
                        Status = discordData[acc0].ContainsKey("status") ? discordData[acc0]["status"] : "",
                        Login = discordData[acc0].ContainsKey("username") ? discordData[acc0]["username"] : ""
                    };
                }

                // Telegram
                if (telegramData.ContainsKey(acc0) && telegramData[acc0].ContainsKey("username"))
                {
                    account.Telegram = new SocialStatus
                    {
                        Status = "ok",
                        Login = telegramData[acc0].ContainsKey("username") ? telegramData[acc0]["username"] : ""
                    };
                }

                socialAccounts.Add(account);
            }

            // ===== ЧАСТЬ 2: Собираем данные Daily Projects =====
            if (log) project.SendInfoToLog("📊 Collecting daily projects data...", false);

            var projectTables = project.TblList();
            var dailyProjects = new List<ProjectData>();

            foreach (var pj in projectTables)
            {
                if (!pj.StartsWith("__")) continue;
                if (pj.StartsWith("__|")) continue;
                var projectData = ProjectData.CollectData(project, pj);
                dailyProjects.Add(projectData);
            }

            // Сортировка
            if (!string.IsNullOrEmpty(sortBy))
            {
                if (log) project.SendInfoToLog($"📊 Sorting projects by: {sortBy}", false);

                switch (sortBy)
                {
                    case "name":
                        dailyProjects = dailyProjects.OrderBy(p => p.ProjectName).ToList();
                        break;

                    case "accountsTotal":
                        dailyProjects = dailyProjects.OrderByDescending(p => p.All.Count).ToList();
                        break;

                    case "rate":
                        dailyProjects = dailyProjects.OrderByDescending(p =>
                        {
                            var success = p.All.Values.Count(v => v[0].Trim() == "+");
                            return p.All.Count > 0 ? (double)success / p.All.Count : 0;
                        }).ToList();
                        break;

                    case "lastActivity":
                    default:
                        dailyProjects = dailyProjects.OrderByDescending(p =>
                        {
                            DateTime latestDate = DateTime.MinValue;

                            foreach (var data in p.All.Values)
                            {
                                var ts = data[1];
                                if (DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime timestamp))
                                {
                                    if (timestamp > latestDate)
                                        latestDate = timestamp;
                                }
                            }

                            return latestDate;
                        }).ToList();
                        break;
                }
            }

            // ===== ЧАСТЬ 3: Генерируем JSON данные =====
            project.GenerateJsonReport(socialAccounts, dailyProjects, log: log);

            // ===== ЧАСТЬ 4: Открываем в браузере если нужно =====
            if (openInBrowser)
            {
                string htmlPath = Path.Combine(project.Path, ".reports", "unionReport.html");

                if (File.Exists(htmlPath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(htmlPath);
                        if (log) project.SendInfoToLog($"📊 Report opened in browser: {htmlPath}", false);
                    }
                    catch (Exception ex)
                    {
                        project.SendWarningToLog($"⚠️ Failed to open report: {ex.Message}", false);
                    }
                }
                else
                {
                    project.SendWarningToLog($"⚠️ HTML report not found at: {htmlPath}", false);
                    project.SendWarningToLog("⚠️ Copy unionReport.html to .reports folder first!", false);
                }
            }

            if (log) project.SendInfoToLog("📊 JSON report generation completed!", false);
        }

        /// <summary>
        /// Быстрое обновление одного проекта (для вызова в конце скрипта)
        /// </summary>
        public static void UpdateSingleProject(this IZennoPosterProjectModel project, 
            string projectName,
            bool log = false)
        {
            if (log) project.SendInfoToLog($"📊 Updating {projectName}.json...", false);
            
            var data = ProjectData.CollectData(project, $"__{projectName}");
            
            string projectsFolder = Path.Combine(project.Path, ".reports", "projects");
            Directory.CreateDirectory(projectsFolder);
            
            var json = new {
                name = projectName,
                timestamp = DateTime.UtcNow,
                accounts = data.All.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new {
                        status = kvp.Value[0].Trim(),
                        timestamp = kvp.Value[1],
                        completionSec = kvp.Value[2].Trim(),
                        report = kvp.Value[3]
                    }
                )
            };
            
            File.WriteAllText(
                Path.Combine(projectsFolder, $"{projectName}.json"),
                JsonConvert.SerializeObject(json, Formatting.Indented)
            );
            
            if (log) project.SendInfoToLog($"✅ {projectName}.json updated!", false);
        }

        // Вспомогательный метод (твой оригинальный код)
        private static Dictionary<int, Dictionary<string, string>> ParseSocialData(
            IZennoPosterProjectModel project, 
            string tableName, 
            string columns, 
            int rangeStart, 
            int rangeEnd)
        {
            var result = new Dictionary<int, Dictionary<string, string>>();
            project.Var("projectTable", tableName);
            var allLines = project.DbGetLines(columns, where: $"id >= {rangeStart} AND id <= {rangeEnd}");

            foreach (var line in allLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split('¦');
                if (parts.Length < 2) continue;

                if (!int.TryParse(parts[0].Trim(), out int accId)) continue;

                var columnNames = columns.Split(',');
                var data = new Dictionary<string, string>();

                for (int i = 1; i < columnNames.Length && i < parts.Length; i++)
                {
                    data[columnNames[i].Trim()] = parts[i].Trim();
                }

                result[accId] = data;
            }

            return result;
        }
    }
}