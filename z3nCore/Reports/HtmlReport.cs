using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
   
   public static class HtmlEncoder
   {
       public static string HtmlEncode(string text)
       {
           if (string.IsNullOrEmpty(text))
               return text;

           return text
               .Replace("&", "&amp;")
               .Replace("<", "&lt;")
               .Replace(">", "&gt;")
               .Replace("\"", "&quot;")
               .Replace("'", "&#39;");
       }

       public static string HtmlAttributeEncode(string text)
       {
           if (string.IsNullOrEmpty(text))
               return text;

           return text
               .Replace("&", "&amp;")
               .Replace("\"", "&quot;")
               .Replace("'", "&#39;")
               .Replace("<", "&lt;")
               .Replace(">", "&gt;");
       }
   }
   
   public class HtmlReport
   {
       
       private readonly IZennoPosterProjectModel _project;
       private readonly Logger _logger;
        public HtmlReport(IZennoPosterProjectModel project, bool log = false)
       {
           _project = project;
           _logger = new Logger(project, log: log, classEmoji: "📊");
       }
        
       public void ShowUnionReport(List<AccountSocialData> socialAccounts, List<ProjectData> dailyProjects, bool call = false)
       {
           _project.SendInfoToLog($"Generating union report: {socialAccounts.Count} social accounts + {dailyProjects.Count} projects...", false);
            string html = GenerateUnionHtml(socialAccounts, dailyProjects);
            string tempPath = System.IO.Path.Combine(_project.Path, ".data", "unionReport.html");
           System.IO.File.WriteAllText(tempPath, html, Encoding.UTF8);
           _project.SendInfoToLog($"Union report saved to: {tempPath}", false);
            if (call) System.Diagnostics.Process.Start(tempPath);
       }
       private string GenerateUnionHtml(List<AccountSocialData> socialAccounts, List<ProjectData> dailyProjects)
       {
           var html = new StringBuilder();
           var userId = _project.ExecuteMacro("{-Environment.CurrentUser-}");
           var title = $"Union Report {DateTime.Now:dd.MM.yyyy [HH:mm:ss]} id: {userId}";
           var today = DateTime.UtcNow.Date;
            // Получаем процессы ZennoPoster
           List<string[]> zennoProcesses = new List<string[]>();
            try
            {
                var zp = Debugger.ZennoProcesses();
                foreach (string[] arr in zp)
                {
                    zennoProcesses.Add(arr);
                }
            }
            catch
            {
            }
           
            // Подсчитываем статистику для Daily
           int totalAccounts = 0;
           int totalSuccess = 0;
           int totalErrors = 0;
           int maxAccountIndex = 0;
            foreach (var project in dailyProjects)
           {
               totalAccounts += project.All.Count;
               foreach (var data in project.All.Values)
               {
                   var status = data[0].Trim();
                   if (status == "+") totalSuccess++;
                   else if (status == "-") totalErrors++;
               }
                foreach (var acc in project.All.Keys)
               {
                   if (int.TryParse(acc, out int accIndex))
                   {
                       if (accIndex > maxAccountIndex)
                           maxAccountIndex = accIndex;
                   }
               }
           }
            var overallSuccessRate = totalAccounts > 0 ? (double)totalSuccess / totalAccounts * 100 : 0;
            // HTML Header
           html.AppendLine("<!DOCTYPE html>");
           html.AppendLine("<html lang='ru'>");
           html.AppendLine("<head>");
           html.AppendLine("    <meta charset='UTF-8'>");
           html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
           html.AppendLine("    <title>" + title + "</title>");
           html.AppendLine("    <style>");
           html.AppendLine(GetUnifiedStyles());
           html.AppendLine("    </style>");
           html.AppendLine("</head>");
           html.AppendLine("<body>");
            html.AppendLine("    <div id='tooltip' class='tooltip'></div>");
           html.AppendLine("    <div class='container'>");
            // Main Header
           html.AppendLine("        <div class='header main-header'>");
           html.AppendLine($"            <h1>📊 {title}</h1>");
           html.AppendLine("        </div>");
            // === SOCIAL REPORT SECTION ===
           html.Append(GenerateSocialReportSection(socialAccounts));
            // Divider
           html.AppendLine("        <div class='section-divider'></div>");
            // === DAILY REPORT SECTION (оригинальная реализация) ===
           html.Append(GenerateDailyReportSection(dailyProjects, today, maxAccountIndex, totalAccounts, totalSuccess, totalErrors, overallSuccessRate, zennoProcesses));
            html.AppendLine("    </div>");
            // JavaScript
           html.AppendLine("    <script>");
           html.AppendLine(GetUnifiedJavaScript());
           html.AppendLine("    </script>");
            html.AppendLine("</body>");
           html.AppendLine("</html>");
            return html.ToString();
       }
        #region Social Report Section
        private string GenerateSocialReportSection(List<AccountSocialData> accounts)
       {
           var sb = new StringBuilder();
            // Подсчет статистики
           int totalTwitter = 0, totalGitHub = 0, totalDiscord = 0, totalTelegram = 0;
           int activeTwitter = 0, activeGitHub = 0, activeDiscord = 0, activeTelegram = 0;
            foreach (var acc in accounts)
           {
               if (acc.Twitter?.IsActive == true) { totalTwitter++; if (acc.Twitter.IsOk) activeTwitter++; }
               if (acc.GitHub?.IsActive == true) { totalGitHub++; if (acc.GitHub.IsOk) activeGitHub++; }
               if (acc.Discord?.IsActive == true) { totalDiscord++; if (acc.Discord.IsOk) activeDiscord++; }
               if (acc.Telegram?.IsActive == true) { totalTelegram++; if (acc.Telegram.IsOk) activeTelegram++; }
           }
            // Section Header
           sb.AppendLine("        <div class='section-header'>");
           sb.AppendLine("            <h2>🌐 Social Networks Status</h2>");
           sb.AppendLine("        </div>");
            // Summary Cards
           sb.AppendLine("        <div class='summary-cards'>");
           sb.AppendLine("            <div class='summary-card'>");
           sb.AppendLine("                <h3>TOTAL ACCOUNTS</h3>");
           sb.AppendLine($"                <div class='value'>{accounts.Count}</div>");
           sb.AppendLine("                <div class='subtext'>Tracked accounts</div>");
           sb.AppendLine("            </div>");
            sb.AppendLine("            <div class='summary-card'>");
           sb.AppendLine("                <h3>TWITTER</h3>");
           sb.AppendLine($"                <div class='value' style='color: #1DA1F2;'>{totalTwitter}</div>");
           sb.AppendLine($"                <div class='subtext'>{activeTwitter} active</div>");
           sb.AppendLine("            </div>");
            sb.AppendLine("            <div class='summary-card'>");
           sb.AppendLine("                <h3>GITHUB</h3>");
           sb.AppendLine($"                <div class='value' style='color: #FFFFFF;'>{totalGitHub}</div>");
           sb.AppendLine($"                <div class='subtext'>{activeGitHub} active</div>");
           sb.AppendLine("            </div>");
            sb.AppendLine("            <div class='summary-card'>");
           sb.AppendLine("                <h3>DISCORD</h3>");
           sb.AppendLine($"                <div class='value' style='color: #5865F2;'>{totalDiscord}</div>");
           sb.AppendLine($"                <div class='subtext'>{activeDiscord} active</div>");
           sb.AppendLine("            </div>");
            sb.AppendLine("            <div class='summary-card'>");
           sb.AppendLine("                <h3>TELEGRAM</h3>");
           sb.AppendLine($"                <div class='value' style='color: #0088CC;'>{totalTelegram}</div>");
           sb.AppendLine($"                <div class='subtext'>{activeTelegram} active</div>");
           sb.AppendLine("            </div>");
           sb.AppendLine("        </div>");
            // Heatmap Section
           sb.AppendLine("        <div class='section'>");
           sb.AppendLine("            <h2>Social Networks HeatMap</h2>");
           sb.AppendLine("            <div class='heatmap-container'>");
           sb.AppendLine("                <div class='heatmap-wrapper'>");
            // Legend
           sb.AppendLine("                    <div class='heatmap-legend'>");
           sb.AppendLine("                        <span>Legend:</span>");
           sb.AppendLine("                        <div class='legend-item'><div class='legend-box' style='background: #1DA1F2;'></div> Twitter</div>");
           sb.AppendLine("                        <div class='legend-item'><div class='legend-box' style='background: #FFFFFF; border-color: #666;'></div> GitHub</div>");
           sb.AppendLine("                        <div class='legend-item'><div class='legend-box' style='background: #5865F2;'></div> Discord</div>");
           sb.AppendLine("                        <div class='legend-item'><div class='legend-box' style='background: #0088CC;'></div> Telegram</div>");
           sb.AppendLine("                        <div class='legend-item'><div class='legend-box' style='opacity: 0.3; background: #666;'></div> Not OK</div>");
           sb.AppendLine("                    </div>");
            // Heatmap cells
           sb.AppendLine("                    <div class='cells-grid social-grid'>");
            foreach (var acc in accounts)
           {
               sb.AppendLine("                        <div class='account-cell'>");
               sb.AppendLine("                            <div class='social-squares'>");
                sb.Append(GenerateSocialSquare("twitter", acc.Twitter, acc.AccountId));
               sb.Append(GenerateSocialSquare("github", acc.GitHub, acc.AccountId));
               sb.Append(GenerateSocialSquare("discord", acc.Discord, acc.AccountId));
               sb.Append(GenerateSocialSquare("telegram", acc.Telegram, acc.AccountId));
                sb.AppendLine("                            </div>");
               sb.AppendLine("                        </div>");
           }
            sb.AppendLine("                    </div>");
           sb.AppendLine("                </div>");
           sb.AppendLine("            </div>");
           sb.AppendLine("        </div>");
            return sb.ToString();
       }
        private string GenerateSocialSquare(string socialName, SocialStatus social, int accountId)
       {
           string color;
           switch (socialName)
           {
               case "twitter":
                   color = "#1DA1F2";
                   break;
               case "github":
                   color = "#FFFFFF";
                   break;
               case "discord":
                   color = "#5865F2";
                   break;
               case "telegram":
                   color = "#0088CC";
                   break;
               default:
                   color = "#30363d";
                   break;
           }
            bool hasData = social?.IsActive == true;
           bool isOk = social?.IsOk == true;
           string opacity = (!hasData || !isOk) ? "opacity: 0.3;" : "";
           string background = hasData ? $"background: {color};" : "background: transparent;";
            string tooltipData = "";
           if (hasData)
           {
               string status = social.IsOk ? "ok" : social.Status;
               tooltipData = $"account #{accountId}||{socialName}||{social.Login}||{status}||social";
           }
           else
           {
               tooltipData = $"account #{accountId}||{socialName}||||not connected||social";
           }
            return $"<div class='social-square' style='{background}{opacity}' " +
                  $"data-social='{socialName}' " +
                  $"data-tooltip='{HtmlEncoder.HtmlAttributeEncode(tooltipData)}'></div>\n";
       }
        #endregion
        #region Daily Report Section (оригинальная реализация из DailyReport)
        private string GenerateDailyReportSection(
           List<ProjectData> projects,
           DateTime today,
           int maxAccountIndex,
           int totalAccounts,
           int totalSuccess,
           int totalErrors,
           double overallSuccessRate,
           List<string[]> zennoProcesses)
       {
           var html = new StringBuilder();
            // Section Header
           html.AppendLine("        <div class='section-header'>");
           html.AppendLine("            <h2>📈 Daily Projects Status</h2>");
           html.AppendLine("        </div>");
            // Summary Cards
           html.AppendLine("        <div class='summary-cards'>");
           html.AppendLine("            <div class='summary-card'>");
           html.AppendLine("                <h3>TOTAL ATTEMPTS</h3>");
           html.AppendLine("                <div class='value'>" + totalAccounts + "</div>");
           html.AppendLine("                <div class='subtext'>In all projects</div>");
           html.AppendLine("            </div>");
           html.AppendLine("            <div class='summary-card success'>");
           html.AppendLine("                <h3>DONE</h3>");
           html.AppendLine("                <div class='value'>" + totalSuccess + "</div>");
           html.AppendLine("                <div class='subtext'>" + overallSuccessRate.ToString("F1") + "% success</div>");
           html.AppendLine("            </div>");
           html.AppendLine("            <div class='summary-card error'>");
           html.AppendLine("                <h3>FAILED</h3>");
           html.AppendLine("                <div class='value'>" + totalErrors + "</div>");
           html.AppendLine("                <div class='subtext'>! Needs attention</div>");
           html.AppendLine("            </div>");
           html.AppendLine("        </div>");
            html.AppendLine("        <div class='section'>");
           html.AppendLine("            <h2>Projects HeatMap</h2>");
           html.AppendLine("            <div class='heatmap-container'>");
           html.AppendLine("                <div class='heatmap-wrapper'>");
            html.AppendLine("                    <div class='heatmap-legend'>");
           html.AppendLine("                        <span>Legend:</span>");
           html.AppendLine("                        <div class='legend-item'><div class='legend-box success'></div><div class='legend-box error'></div> Today</div>");
           html.AppendLine("                        <div class='legend-item'><div class='legend-box success-yesterday'></div><div class='legend-box error-yesterday'></div> Yesterday</div>");
           html.AppendLine("                        <div class='legend-item'><div class='legend-box success-2days'></div><div class='legend-box error-2days'></div> 2 days ago</div>");
           html.AppendLine("                        <div class='legend-item'><div class='legend-box success-old'></div><div class='legend-box error-old'></div> 3+ days</div>");
           html.AppendLine("                        <div class='legend-item'><div class='legend-box notdone'></div> Not touched</div>");
           html.AppendLine("                    </div>");
            html.AppendLine("                    <div class='heatmap-grid'>");
           html.AppendLine("                        <div class='heatmaps-column'>");
            foreach (var proj in projects)
           {
               if (proj.All.Count == 0) continue;
                int successCount = 0;
               int errorCount = 0;
               double totalSuccessTime = 0;
               double totalErrorTime = 0;
               int successWithTime = 0;
               int errorWithTime = 0;
               double minSuccessTime = double.MaxValue;
               double maxSuccessTime = 0;
               double minErrorTime = double.MaxValue;
               double maxErrorTime = 0;
                foreach (var data in proj.All.Values)
               {
                   var status = data[0].Trim();
                   var ts = data[1];
                   var timeStr = data[2].Trim();
                    bool isToday = false;
                   if (DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime timestamp))
                   {
                       isToday = timestamp.Date == today;
                   }
                    if (status == "+")
                   {
                       if (isToday) successCount++;
                       if (!string.IsNullOrEmpty(timeStr) && double.TryParse(timeStr,
                               System.Globalization.NumberStyles.Any,
                               System.Globalization.CultureInfo.InvariantCulture, out double time) && time > 0)
                       {
                           totalSuccessTime += time;
                           successWithTime++;
                           if (time < minSuccessTime) minSuccessTime = time;
                           if (time > maxSuccessTime) maxSuccessTime = time;
                       }
                   }
                   else if (status == "-")
                   {
                       if (isToday) errorCount++;
                       if (!string.IsNullOrEmpty(timeStr) && double.TryParse(timeStr,
                               System.Globalization.NumberStyles.Any,
                               System.Globalization.CultureInfo.InvariantCulture, out double time) && time > 0)
                       {
                           totalErrorTime += time;
                           errorWithTime++;
                           if (time < minErrorTime) minErrorTime = time;
                           if (time > maxErrorTime) maxErrorTime = time;
                       }
                   }
               }
                var successRate = maxAccountIndex > 0 ? (double)successCount / maxAccountIndex * 100 : 0;
               var errorRate = maxAccountIndex > 0 ? (double)errorCount / maxAccountIndex * 100 : 0;
               var avgSuccessTime = successWithTime > 0 ? totalSuccessTime / successWithTime : 0;
               var avgErrorTime = errorWithTime > 0 ? totalErrorTime / errorWithTime : 0;
               var statusClass = successRate >= 90 ? "stat-good" : (successRate >= 70 ? "" : "stat-bad");
                html.AppendLine("                        <div class='heatmap-with-stats'>");
                html.AppendLine("                            <div class='heatmap-project-card'>");
               html.AppendLine("                                <div class='project-card'>");
               html.AppendLine("                                    <div class='project-name'>" + proj.ProjectName + "</div>");
               html.AppendLine("                                    <div class='progress-bar'>");
               html.AppendLine("                                        <div style='display: flex; height: 100%; width: 100%;'>");
               html.AppendLine("                                            <div style='width: " + successRate.ToString("F1") + "%; background: #238636;'></div>");
               html.AppendLine("                                            <div style='width: " + errorRate.ToString("F1") + "%; background: #da3633;'></div>");
               html.AppendLine("                                        </div>");
               html.AppendLine("                                    </div>");
                html.AppendLine("                                    <div class='project-stats'>");
               html.AppendLine("                                        <div class='stat-row'>");
               html.AppendLine("                                            <span>✔️ Successful: </span>");
               html.AppendLine("                                            <span class='stat-good'>" + successCount + "</span>");
               html.AppendLine("                                        </div>");
                if (successWithTime > 0)
               {
                   html.AppendLine("                                        <div class='stat-row'>");
                   html.AppendLine("                                            <span>Min|Max|Avg : </span>");
                   html.AppendLine("                                            <span class='stat-neutral'>" +
                                   minSuccessTime.ToString("F1") + "|" +
                                   maxSuccessTime.ToString("F1") + "|" +
                                   avgSuccessTime.ToString("F1") + "s</span>");
                   html.AppendLine("                                        </div>");
               }
                html.AppendLine("                                        <div class='stat-row'>");
               html.AppendLine("                                            <span>❌ Failed:  </span>");
               html.AppendLine("                                            <span class='stat-bad'>" + errorCount + "</span>");
               html.AppendLine("                                        </div>");
                if (errorWithTime > 0)
               {
                   html.AppendLine("                                        <div class='stat-row'>");
                   html.AppendLine("                                            <span>Min|Max|Avg : </span>");
                   html.AppendLine("                                            <span class='stat-neutral'>" +
                                   minErrorTime.ToString("F1") + "|" +
                                   maxErrorTime.ToString("F1") + "|" +
                                   avgErrorTime.ToString("F1") + "s</span>");
                   html.AppendLine("                                        </div>");
               }
                html.AppendLine("                                        <div class='stat-row'>");
               html.AppendLine("                                            <span>[✔️/❌] Rate: </span>");
               html.AppendLine("                                            <span class='" + statusClass + "'>" + successRate.ToString("F1") + "%</span>");
               html.AppendLine("                                        </div>");
                html.AppendLine("                                    </div>");
               html.AppendLine("                                </div>");
               html.AppendLine("                            </div>");
                html.AppendLine("                            <div class='heatmap-content'>");
               html.AppendLine("                                <div class='heatmap-row'>");
               html.AppendLine("                                    <div class='cells-container'>");
                for (int i = 1; i <= maxAccountIndex; i++)
               {
                   var accStr = i.ToString();
                   var cellClass = "heatmap-cell";
                   var tooltipData = "";
                    if (proj.All.ContainsKey(accStr))
                   {
                       var data = proj.All[accStr];
                       var status = data[0].Trim();
                       var ts = data[1];
                       var completionTime = data[2];
                       var report = data[3];
                        string ageClass = "";
                       if (DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime timestamp))
                       {
                           var recordDate = timestamp.ToUniversalTime().Date;
                           var daysDiff = (today - recordDate).Days;
                            if (daysDiff == 0)
                               ageClass = "";
                           else if (daysDiff == 1)
                               ageClass = "-yesterday";
                           else if (daysDiff == 2)
                               ageClass = "-2days";
                           else
                               ageClass = "-old";
                       }
                        if (status == "+")
                       {
                           cellClass += " success" + ageClass;
                       }
                       else if (status == "-")
                       {
                           cellClass += " error" + ageClass;
                       }
                        tooltipData = "account #" + accStr + "||" +
                                     proj.ProjectName + "||" +
                                     ts + "||" +
                                     completionTime + "||" +
                                     (status == "+" ? "success" : "error") + "||" +
                                     report + "||daily";
                   }
                   else
                   {
                       tooltipData = "account #" + accStr + "||" +
                                     proj.ProjectName + "||" +
                                     "—||" +
                                     "||" +
                                     "notdone||||daily";
                   }
                    html.AppendLine("                                        <div class='" + cellClass +
                                   "' data-tooltip='" +
                                   HtmlEncoder.HtmlAttributeEncode(tooltipData) + "'></div>");
               }
                html.AppendLine("                                    </div>");
               html.AppendLine("                                </div>");
               html.AppendLine("                            </div>");
               html.AppendLine("                        </div>");
           }
            html.AppendLine("                        </div>");
            // Stats sidebar (процессы справа)
           html.AppendLine("                        <div class='stats-sidebar'>");
           html.AppendLine("                            <div class='stats-card'>");
           html.AppendLine("                                <h3>ZP Processes</h3>");
            if (zennoProcesses.Count > 0)
           {
               html.AppendLine("                                <div class='processes-list'>");
                foreach (var proc in zennoProcesses)
               {
                   var name = proc[0];
                   var mem = proc[1];
                   var time = proc[2];
                    html.AppendLine("                                    <div class='process-line' title='" +
                                   HtmlEncoder.HtmlAttributeEncode(name) + "'>");
                   html.Append("                                        <span class='process-name'>" +
                               HtmlEncoder.HtmlEncode(name) + "</span> ");
                   html.Append("<span class='process-mem'>" + mem + "mb</span> ");
                   html.AppendLine("<span class='process-time'>" + HtmlEncoder.HtmlEncode(time) + "min</span>");
                   html.AppendLine("                                    </div>");
               }
                html.AppendLine("                                </div>");
           }
           else
           {
               html.AppendLine("                                <div style='color: #8b949e; font-size: 8px;'>Нет данных</div>");
           }
            html.AppendLine("                            </div>");
           html.AppendLine("                        </div>");
           html.AppendLine("                    </div>");
           html.AppendLine("                </div>");
           html.AppendLine("            </div>");
           html.AppendLine("        </div>");
            // Empty projects
           var emptyProjects = projects.Where(p => p.All.Count == 0).ToList();
           if (emptyProjects.Count > 0)
           {
               html.AppendLine("        <div class='section'>");
               html.AppendLine("            <h2>💤 Idle Projects</h2>");
               html.AppendLine("            <div class='project-grid'>");
                foreach (var project in emptyProjects)
               {
                   html.AppendLine("                <div class='project-card'>");
                   html.AppendLine("                    <div class='project-name'>" + project.ProjectName + "</div>");
                   html.AppendLine("                    <div style='color: #8b949e; font-size: 12px;'>no data</div>");
                   html.AppendLine("                </div>");
               }
                html.AppendLine("            </div>");
               html.AppendLine("        </div>");
           }
            return html.ToString();
       }
        #endregion
        #region Styles and JavaScript
        private string GetUnifiedStyles()
       {
           return @"
               * { margin: 0; padding: 0; box-sizing: border-box; }
               body {
                   font-family: 'Iosevka', 'Consolas', monospace;
                   background: #0d1117;
                   padding: 15px;
                   color: #c9d1d9;
               }
               .container { max-width: 1900px; margin: 0 auto; }
                /* Headers */
               .header {
                   background: #161b22;
                   border: 1px solid #30363d;
                   color: #c9d1d9;
                   padding: 12px 20px;
                   border-radius: 6px;
                   margin-bottom: 15px;
                   display: flex;
                   justify-content: space-between;
                   align-items: center;
               }
               .header h1 { font-size: 18px; font-weight: 600; }
               .main-header {
                   background: linear-gradient(135deg, #161b22 0%, #1c2128 100%);
                   border: 2px solid #58a6ff;
               }
               .section-header {
                   background: #0d1117;
                   border-left: 4px solid #58a6ff;
                   padding: 10px 15px;
                   margin: 20px 0 15px 0;
               }
               .section-header h2 {
                   font-size: 16px;
                   color: #58a6ff;
                   font-weight: 600;
               }
               .section-divider {
                   height: 2px;
                   background: linear-gradient(90deg, transparent, #30363d, transparent);
                   margin: 30px 0;
               }
                /* Summary Cards */
               .summary-cards {
                   display: flex;
                   gap: 10px;
                   margin-bottom: 15px;
               }
               .summary-card {
                   background: #161b22;
                   padding: 10px 15px;
                   border-radius: 6px;
                   border: 1px solid #30363d;
                   flex: 1;
                   min-width: 0;
               }
               .summary-card h3 { color: #8b949e; font-size: 11px; margin-bottom: 4px; }
               .summary-card .value { font-size: 20px; font-weight: 600; color: #c9d1d9; }
               .summary-card .subtext { color: #8b949e; font-size: 10px; margin-top: 2px; }
                /* Sections */
               .section {
                   background: #161b22;
                   border: 1px solid #30363d;
                   border-radius: 6px;
                   padding: 15px;
                   margin-bottom: 15px;
               }
               .section h2 {
                   margin-bottom: 12px;
                   color: #c9d1d9;
                   border-bottom: 1px solid #30363d;
                   padding-bottom: 8px;
                   font-size: 14px;
                   font-weight: 600;
               }
                /* Heatmap Common */
               .heatmap-container {
                   overflow-x: auto;
                   padding: 5px 0;
               }
               .heatmap-wrapper {
                   display: inline-block;
                   min-width: 100%;
               }
               .heatmap-legend {
                   display: flex;
                   align-items: center;
                   flex-wrap: wrap;
                   gap: 15px;
                   margin-bottom: 15px;
                   font-size: 11px;
                   color: #8b949e;
               }
               .legend-item {
                   display: flex;
                   align-items: center;
                   gap: 5px;
               }
               .legend-box {
                   width: 12px;
                   height: 12px;
                   border-radius: 2px;
                   border: 1px solid #30363d;
               }
               .legend-box.success { background: #238636; }
               .legend-box.error { background: #da3633; }
               .legend-box.success-yesterday { background: #1a5c28; }
               .legend-box.error-yesterday { background: #a32a27; }
               .legend-box.success-2days { background: #123819; }
               .legend-box.error-2days { background: #6b1e1d; }
               .legend-box.success-old { background: #0a1a0d; }
               .legend-box.error-old { background: #3a1210; }
               .legend-box.notdone { background: transparent; }
                /* Social Network Cells */
               .cells-grid.social-grid {
                   display: flex;
                   flex-wrap: wrap;
                   gap: 2px;
                   max-width: 100%;
               }
               .account-cell {
                   width: calc((100vw - 92px) / 100 - 2px);
                   height: calc((100vw - 92px) / 100 - 2px);
                   background: #0d1117;
                   border: 1px solid #30363d;
                   border-radius: 2px;
                   padding: 1px;
                   position: relative;
                   cursor: pointer;
                   transition: all 0.2s;
               }
               .account-cell:hover {
                   transform: scale(1.3);
                   z-index: 10;
                   box-shadow: 0 0 8px rgba(255,255,255,0.3);
               }
               .social-squares {
                   display: grid;
                   grid-template-columns: 1fr 1fr;
                   grid-template-rows: 1fr 1fr;
                   gap: 1px;
                   width: 100%;
                   height: 100%;
               }
               .social-square {
                   border-radius: 1px;
                   cursor: pointer;
                   transition: none;
                   position: relative;
                   min-width: 0;
                   min-height: 0;
               }
                /* Daily Report Cells (оригинальные стили из DailyReport) */
               .heatmap-grid {
                   display: flex;
                   gap: 15px;
               }
               .heatmaps-column {
                   flex: 1;
                   display: flex;
                   flex-direction: column;
                   gap: 15px;
                   min-width: 0;
               }
               .heatmap-with-stats {
                   display: flex;
                   gap: 12px;
                   align-items: stretch;
               }
               .heatmap-content {
                   flex: 1;
                   min-width: 0;
               }
               .heatmap-project-card {
                   width: 200px;
                   flex-shrink: 0;
                   display: flex;
                   flex-direction: column;
               }
               .heatmap-row {
                   display: flex;
                   align-items: center;
                   gap: 8px;
               }
               .cells-container {
                   display: flex;
                   flex-wrap: wrap;
                   gap: 2px;
                   max-width: calc((11px + 2px) * 100);
               }
               .heatmap-cell {
                   width: 11px;
                   height: 11px;
                   border-radius: 2px;
                   border: 1px solid #30363d;
                   background: transparent;
                   cursor: pointer;
                   transition: all 0.2s;
                   position: relative;
               }
               .heatmap-cell.success { background: #238636; border-color: #2ea043; }
               .heatmap-cell.error { background: #da3633; border-color: #f85149; }
               .heatmap-cell.success-yesterday { background: #1a5c28; border-color: #247a35; }
               .heatmap-cell.error-yesterday { background: #a32a27; border-color: #c93a36; }
               .heatmap-cell.success-2days { background: #123819; border-color: #1a5024; }
               .heatmap-cell.error-2days { background: #6b1e1d; border-color: #8d2826; }
               .heatmap-cell.success-old { background: #0a1a0d; border-color: #112b15; }
               .heatmap-cell.error-old { background: #3a1210; border-color: #5a1a18; }
               .heatmap-cell:hover {
                   transform: scale(1.3);
                   z-index: 10;
                   box-shadow: 0 0 8px rgba(255,255,255,0.3);
               }
                /* Stats Sidebar (процессы справа) */
               .stats-sidebar {
                   width: 200px;
                   flex-shrink: 0;
                   display: flex;
                   flex-direction: column;
                   gap: 12px;
               }
               .stats-card {
                   background: #0d1117;
                   border: 1px solid #30363d;
                   border-radius: 6px;
                   padding: 12px;
               }
               .stats-card h3 {
                   color: #c9d1d9;
                   font-size: 12px;
                   margin-bottom: 8px;
                   font-weight: 600;
               }
               .processes-list {
                   display: flex;
                   flex-direction: column;
                   gap: 4px;
                   font-size: 9px;
                   font-family: 'Iosevka', 'Consolas', monospace;
               }
               .process-line {
                   display: flex;
                   align-items: center;
                   padding: 4px;
                   background: rgba(48, 54, 61, 0.3);
                   border-radius: 3px;
                   line-height: 1.3;
               }
               .process-name {
                   flex: 1;
                   min-width: 0;
                   overflow: hidden;
                   text-overflow: ellipsis;
                   white-space: nowrap;
                   color: #c9d1d9;
               }
               .process-mem {
                   min-width: 35px;
                   text-align: right;
                   color: #58a6ff;
                   margin-left: 4px;
               }
               .process-time {
                   min-width: 50px;
                   text-align: right;
                   color: #8b949e;
               }
                /* Project Cards */
               .project-grid {
                   display: grid;
                   grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
                   gap: 15px;
                   margin-bottom: 20px;
               }
               .project-card {
                   border: 1px solid #30363d;
                   border-radius: 6px;
                   padding: 10px;
                   background: #0d1117;
                   transition: all 0.2s;
                   height: 100%;
               }
               .project-card:hover {
                   border-color: #58a6ff;
               }
               .project-name {
                   font-weight: 600;
                   color: #c9d1d9;
                   margin-bottom: 6px;
                   font-size: 12px;
                   line-height: 1.3;
                   word-wrap: break-word;
               }
               .progress-bar {
                   height: 4px;
                   background: #21262d;
                   border-radius: 4px;
                   overflow: hidden;
                   margin: 6px 0;
               }
               .project-stats {
                   display: flex;
                   flex-direction: column;
                   gap: 3px;
                   font-size: 10px;
                   color: #8b949e;
                   margin-top: 6px;
               }
               .stat-row {
                   display: flex;
                   justify-content: space-between;
                   align-items: center;
               }
               .stat-good { color: #3fb950; font-weight: 600; }
               .stat-bad { color: #f85149; font-weight: 600; }
               .stat-neutral { color: #8b949e; font-weight: 600; }
                /* Tooltip */
               .tooltip {
                   position: absolute;
                   background: #1c2128;
                   border: 1px solid #30363d;
                   border-radius: 6px;
                   padding: 12px;
                   color: #c9d1d9;
                   font-size: 12px;
                   white-space: pre-wrap;
                   pointer-events: none;
                   z-index: 1000;
                   box-shadow: 0 8px 24px rgba(0,0,0,0.5);
                   display: none;
                   max-width: 400px;
                   line-height: 1.5;
               }
               .tooltip.show { display: block; }
               .tooltip-title {
                   font-weight: 600;
                   margin-bottom: 8px;
                   color: #58a6ff;
                   font-size: 13px;
               }
               .tooltip-social {
                   color: #8b949e;
                   font-size: 11px;
                   margin-bottom: 6px;
                   text-transform: capitalize;
               }
               .tooltip-time {
                   color: #8b949e;
                   font-size: 11px;
                   margin-bottom: 8px;
               }
               .tooltip-login {
                   font-family: 'Iosevka', monospace;
                   font-size: 12px;
                   font-weight: bold;
                   margin: 6px 0;
                   padding: 4px 8px;
                   border-radius: 4px;
                   background: rgba(88, 166, 255, 0.1);
                   color: #58a6ff;
               }
               .tooltip-status {
                   margin: 5px 0;
                   padding: 4px 8px;
                   border-radius: 4px;
                   display: inline-block;
                   font-size: 11px;
               }
               .tooltip-status.ok {
                   color: #3fb950;
                   background: rgba(63, 185, 80, 0.1);
               }
               .tooltip-status.success {
                   color: #3fb950;
                   background: rgba(63, 185, 80, 0.1);
               }
               .tooltip-status.error {
                   color: #f85149;
                   background: rgba(248, 81, 73, 0.1);
               }
               .tooltip-status.empty { color: #8b949e; }
               .tooltip-error {
                   margin-top: 8px;
                   color: #f85149;
                   font-family: 'Courier New', monospace;
                   font-size: 11px;
                   padding: 8px;
                   background: rgba(248, 81, 73, 0.05);
                   border-radius: 4px;
                   border-left: 3px solid #f85149;
               }
               .tooltip-info {
                   margin-top: 8px;
                   color: #8b949e;
                   font-family: 'Courier New', monospace;
                   font-size: 11px;
                   padding: 8px;
                   background: rgba(139, 148, 158, 0.05);
                   border-radius: 4px;
                   border-left: 3px solid #8b949e;
               }
                @media (max-width: 768px) {
                   .summary-cards { flex-direction: column; }
                   .cells-grid { justify-content: center; }
                   .heatmap-grid { flex-direction: column; }
                   .heatmap-with-stats { flex-direction: column; }
                   .heatmap-project-card { width: 100%; max-width: 320px; }
                   .stats-sidebar { width: 100%; }
                   .project-grid { grid-template-columns: 1fr; }
               }
           ";
       }
        private string GetUnifiedJavaScript()
       {
           return @"
               const tooltip = document.getElementById('tooltip');
               const allCells = document.querySelectorAll('.social-square, .heatmap-cell');
                allCells.forEach(cell => {
                   cell.addEventListener('mouseenter', function(e) {
                       const data = this.getAttribute('data-tooltip');
                       if (!data) return;
                        const parts = data.split('||');
                       const type = parts[parts.length - 1]; // 'social' or 'daily'
                        let content = '';
                       if (type === 'social') {
                           // Social tooltip
                           const account = parts[0];
                           const social = parts[1];
                           const login = parts[2];
                           const status = parts[3];
                            content = '<div class=""tooltip-title"">' + account + '</div>';
                           content += '<div class=""tooltip-social"">' + social + '</div>';
                            if (login && login !== '') {
                               content += '<div class=""tooltip-login"">' + login + '</div>';
                               if (status === 'ok') {
                                   content += '<div class=""tooltip-status ok"">✓ Status: OK</div>';
                               } else if (status === 'not connected') {
                                   content += '<div class=""tooltip-status empty"">Not connected</div>';
                               } else {
                                   content += '<div class=""tooltip-status error"">✗ Status: ' + status + '</div>';
                               }
                           } else {
                               content += '<div class=""tooltip-status empty"">No data</div>';
                           }
                       } else {
                           // Daily tooltip
                           const acc = parts[0];
                           const project = parts[1];
                           const time = parts[2];
                           const completionTime = parts[3];
                           const status = parts[4];
                           const report = parts[5] || '';
                            content = '<div class=""tooltip-title"">' + acc + '</div>';
                           content += '<div style=""color: #8b949e; margin-bottom: 5px;"">' + project + '</div>';
                            if (time !== '—') {
                               content += '<div class=""tooltip-time"">⏱ ' + time;
                               if (completionTime && completionTime !== '') {
                                   content += ' (' + completionTime + 's)';
                               }
                               content += '</div>';
                           }
                            if (status === 'success') {
                               content += '<div class=""tooltip-status success"">✓ Success</div>';
                           } else if (status === 'error') {
                               content += '<div class=""tooltip-status error"">✗ Failed</div>';
                           } else {
                               content += '<div style=""color: #8b949e; font-size: 11px;"">notTouched</div>';
                           }
                            if (report && report.trim() !== '') {
                               var reportClass = status === 'error' ? 'tooltip-error' : 'tooltip-info';
                               content += '<div class=""' + reportClass + '"">' + report.replace(/\n/g, '<br>') + '</div>';
                           }
                       }
                        tooltip.innerHTML = content;
                       tooltip.classList.add('show');
                        const rect = this.getBoundingClientRect();
                       const tooltipRect = tooltip.getBoundingClientRect();
                        let left = rect.left + window.scrollX + rect.width / 2 - tooltipRect.width / 2;
                       let top = rect.top + window.scrollY - tooltipRect.height - 10;
                        if (left < 10) left = 10;
                       if (left + tooltipRect.width > window.innerWidth - 10) {
                           left = window.innerWidth - tooltipRect.width - 10;
                       }
                       if (top < 10) {
                           top = rect.bottom + window.scrollY + 10;
                       }
                        tooltip.style.left = left + 'px';
                       tooltip.style.top = top + 'px';
                   });
                    cell.addEventListener('mouseleave', function() {
                       tooltip.classList.remove('show');
                   });
                    cell.addEventListener('click', function(e) {
                       const data = this.getAttribute('data-tooltip');
                       if (!data) return;
                        const parts = data.split('||');
                       const type = parts[parts.length - 1];
                        let copyText = '';
                       if (type === 'social') {
                           const account = parts[0];
                           const social = parts[1];
                           const login = parts[2];
                           const status = parts[3];
                            copyText = account + '\n' + social;
                           if (login && login !== '') {
                               copyText += '\n' + login;
                               copyText += '\nStatus: ' + status;
                           } else {
                               copyText += '\nNot connected';
                           }
                       } else {
                           const acc = parts[0];
                           const project = parts[1];
                           const time = parts[2];
                           const completionTime = parts[3];
                           const status = parts[4];
                           const report = parts[5] || '';
                            copyText = acc + '\n' + project;
                           if (time !== '—') {
                               copyText += '\n' + time;
                               if (completionTime && completionTime !== '') {
                                   copyText += ' (' + completionTime + 's)';
                               }
                           }
                            if (status === 'success') {
                               copyText += '\nStatus: Success';
                           } else if (status === 'error') {
                               copyText += '\nStatus: Failed';
                               if (report && report.trim() !== '') {
                                   copyText += '\n\nError:\n' + report;
                               }
                           } else {
                               copyText += '\nStatus: notTouched';
                           }
                       }
                        navigator.clipboard.writeText(copyText).then(function() {
                           const originalBorder = cell.style.border;
                           cell.style.border = '2px solid #58a6ff';
                           setTimeout(function() {
                               cell.style.border = originalBorder;
                           }, 300);
                       }).catch(function(err) {
                           console.error('Copy error:', err);
                       });
                   });
               });
           ";
       }
        #endregion
        

   }

}


namespace z3nCore
{
   using Utilities;
   public static partial class ProjectExtensions
   {
       private static Dictionary<int, Dictionary<string, string>> ParseSocialData(IZennoPosterProjectModel project, string tableName, string columns, int rangeStart, int rangeEnd)
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

       public static void GenerateHtmlReport(this IZennoPosterProjectModel project, List<AccountSocialData> socialAccounts, List<ProjectData> dailyProjects, bool call = false)
       {
           new HtmlReport(project, log: false).ShowUnionReport(socialAccounts, dailyProjects, call);
       }
       public static void GenerateFullHtmlReport(this IZennoPosterProjectModel project)
        {
            var rangeStart = project.Int("rangeStart");
            var rangeEnd = 1000;//project.Int("rangeEnd");
            
            // Получаем ВСЕ данные за 4 запроса вместо (rangeEnd - rangeStart) * 4
            var twitterData = ParseSocialData(project, "_twitter", "id, status, login", rangeStart, rangeEnd);
            var githubData = ParseSocialData(project, "_github", "id, status, login", rangeStart, rangeEnd);
            var discordData = ParseSocialData(project, "_discord", "id, status, username", rangeStart, rangeEnd);
            var telegramData = ParseSocialData(project, "_telegram", "id, username", rangeStart, rangeEnd);
            
            // Теперь просто собираем объекты
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
            
            // ===== ЧАСТЬ 2: Собираем данные Daily Projects (без изменений) =====
            var projectTables = project.TblList();
            var dailyProjects = new List<ProjectData>();
            
            foreach (var pj in projectTables)
            {
                if (!pj.StartsWith("__")) continue;
                if (pj.StartsWith("__|")) continue;
                var projectData = ProjectData.CollectData(project, pj);
                dailyProjects.Add(projectData);
            }
            
            // ===== ЧАСТЬ 3: Генерируем объединённый отчёт (без изменений) =====
            project.GenerateHtmlReport(socialAccounts, dailyProjects, call: false);
        }
        
   }

}