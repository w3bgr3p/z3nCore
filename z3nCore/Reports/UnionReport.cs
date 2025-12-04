using System;
using System.Collections.Generic;
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

   public class UnionReport
   {
       private readonly IZennoPosterProjectModel _project;
       private readonly Logger _logger;
       public UnionReport(IZennoPosterProjectModel project, bool log = false)
       {
           _project = project;
           _logger = new Logger(project, log: log, classEmoji: "📊");
       }
       /// <summary>
       /// Генерирует объединённый отчёт: Social Networks (сверху) + Daily Report (снизу)
       /// </summary>
       public void ShowUnionReport(
           List<AccountSocialData> socialAccounts,
           List<DailyReport.ProjectData> dailyProjects,
           bool call = false)
       {
           _project.SendInfoToLog($"Generating union report: {socialAccounts.Count} social accounts + {dailyProjects.Count} projects...", false);
           string html = GenerateUnionHtml(socialAccounts, dailyProjects);
           string tempPath = System.IO.Path.Combine(_project.Path, ".data", "unionReport.html");
           System.IO.File.WriteAllText(tempPath, html, Encoding.UTF8);
           _project.SendInfoToLog($"Union report saved to: {tempPath}", false);
           if (call) System.Diagnostics.Process.Start(tempPath);
       }
       private string GenerateUnionHtml(
           List<AccountSocialData> socialAccounts,
           List<DailyReport.ProjectData> dailyProjects)
       {
           var sb = new StringBuilder();
           var userId = _project.ExecuteMacro("{-Environment.CurrentUser-}");
           var title = $"Union Report {DateTime.Now:dd.MM.yyyy [HH:mm:ss]} id: {userId}";
           // HTML Header
           sb.AppendLine("<!DOCTYPE html>");
           sb.AppendLine("<html lang='ru'>");
           sb.AppendLine("<head>");
           sb.AppendLine("    <meta charset='UTF-8'>");
           sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
           sb.AppendLine("    <title>" + title + "</title>");
           sb.AppendLine("    <style>");
           sb.AppendLine(GetUnifiedStyles());
           sb.AppendLine("    </style>");
           sb.AppendLine("</head>");
           sb.AppendLine("<body>");
           sb.AppendLine("    <div id='tooltip' class='tooltip'></div>");
           sb.AppendLine("    <div class='container'>");
           // Main Header
           sb.AppendLine("        <div class='header main-header'>");
           sb.AppendLine($"            <h1>📊 {title}</h1>");
           sb.AppendLine("        </div>");
           // === SOCIAL REPORT SECTION ===
           sb.Append(GenerateSocialReportSection(socialAccounts));
           // Divider
           sb.AppendLine("        <div class='section-divider'></div>");
           // === DAILY REPORT SECTION ===
           sb.Append(GenerateDailyReportSection(dailyProjects));
           sb.AppendLine("    </div>");
           // JavaScript
           sb.AppendLine("    <script>");
           sb.AppendLine(GetUnifiedJavaScript());
           sb.AppendLine("    </script>");
           sb.AppendLine("</body>");
           sb.AppendLine("</html>");
           return sb.ToString();
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
                  $"data-tooltip='{DailyReport.HtmlEncoder.HtmlAttributeEncode(tooltipData)}'></div>\n";
       }
       #endregion
       #region Daily Report Section
       private string GenerateDailyReportSection(List<DailyReport.ProjectData> projects)
       {
           var sb = new StringBuilder();
           var today = DateTime.UtcNow.Date;
           int totalAccounts = 0;
           int totalSuccess = 0;
           int totalErrors = 0;
           foreach (var project in projects)
           {
               totalAccounts += project.All.Count;
               foreach (var data in project.All.Values)
               {
                   var status = data[0].Trim();
                   if (status == "+") totalSuccess++;
                   else if (status == "-") totalErrors++;
               }
           }
           var overallSuccessRate = totalAccounts > 0 ? (double)totalSuccess / totalAccounts * 100 : 0;
           var maxAccountIndex = 0;
           foreach (var project in projects)
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
           // Section Header
           sb.AppendLine("        <div class='section-header'>");
           sb.AppendLine("            <h2>📈 Daily Projects Status</h2>");
           sb.AppendLine("        </div>");
           // Summary Cards
           sb.AppendLine("        <div class='summary-cards'>");
           sb.AppendLine("            <div class='summary-card'>");
           sb.AppendLine("                <h3>TOTAL ATTEMPTS</h3>");
           sb.AppendLine($"                <div class='value'>{totalAccounts}</div>");
           sb.AppendLine("                <div class='subtext'>In all projects</div>");
           sb.AppendLine("            </div>");
           sb.AppendLine("            <div class='summary-card success'>");
           sb.AppendLine("                <h3>DONE</h3>");
           sb.AppendLine($"                <div class='value'>{totalSuccess}</div>");
           sb.AppendLine($"                <div class='subtext'>{overallSuccessRate:F1}% success</div>");
           sb.AppendLine("            </div>");
           sb.AppendLine("            <div class='summary-card error'>");
           sb.AppendLine("                <h3>FAILED</h3>");
           sb.AppendLine($"                <div class='value'>{totalErrors}</div>");
           sb.AppendLine("                <div class='subtext'>! Needs attention</div>");
           sb.AppendLine("            </div>");
           sb.AppendLine("        </div>");
           // Heatmap Section
           sb.AppendLine("        <div class='section'>");
           sb.AppendLine("            <h2>Projects HeatMap</h2>");
           sb.AppendLine("            <div class='heatmap-container'>");
           sb.AppendLine("                <div class='heatmap-wrapper'>");
           // Legend
           sb.AppendLine("                    <div class='heatmap-legend'>");
           sb.AppendLine("                        <span>Legend:</span>");
           sb.AppendLine("                        <div class='legend-item'><div class='legend-box success'></div><div class='legend-box error'></div> Today</div>");
           sb.AppendLine("                        <div class='legend-item'><div class='legend-box success-yesterday'></div><div class='legend-box error-yesterday'></div> Yesterday</div>");
           sb.AppendLine("                        <div class='legend-item'><div class='legend-box success-2days'></div><div class='legend-box error-2days'></div> 2 days ago</div>");
           sb.AppendLine("                        <div class='legend-item'><div class='legend-box success-old'></div><div class='legend-box error-old'></div> 3+ days</div>");
           sb.AppendLine("                        <div class='legend-item'><div class='legend-box notdone'></div> Not touched</div>");
           sb.AppendLine("                    </div>");
           sb.AppendLine("                    <div class='heatmap-grid'>");
           sb.AppendLine("                        <div class='heatmaps-column'>");
           foreach (var proj in projects)
           {
               if (proj.All.Count == 0) continue;
               var stats = CalculateProjectStats(proj, today, maxAccountIndex);
               sb.AppendLine("                        <div class='heatmap-with-stats'>");
               sb.AppendLine("                            <div class='heatmap-project-card'>");
               sb.AppendLine("                                <div class='project-card'>");
               sb.AppendLine($"                                    <div class='project-name'>{proj.ProjectName}</div>");
               sb.AppendLine("                                    <div class='progress-bar'>");
               sb.AppendLine("                                        <div style='display: flex; height: 100%; width: 100%;'>");
               sb.AppendLine($"                                            <div style='width: {stats.SuccessRate:F1}%; background: #238636;'></div>");
               sb.AppendLine($"                                            <div style='width: {stats.ErrorRate:F1}%; background: #da3633;'></div>");
               sb.AppendLine("                                        </div>");
               sb.AppendLine("                                    </div>");
               sb.Append(GenerateProjectStatsHtml(stats));
               sb.AppendLine("                                </div>");
               sb.AppendLine("                            </div>");
               sb.AppendLine("                            <div class='heatmap-content'>");
               sb.AppendLine("                                <div class='heatmap-row'>");
               sb.AppendLine("                                    <div class='cells-container'>");
               for (int i = 1; i <= maxAccountIndex; i++)
               {
                   sb.Append(GenerateDailyCell(proj, i.ToString(), today));
               }
               sb.AppendLine("                                    </div>");
               sb.AppendLine("                                </div>");
               sb.AppendLine("                            </div>");
               sb.AppendLine("                        </div>");
           }
           sb.AppendLine("                        </div>");
           sb.AppendLine("                    </div>");
           sb.AppendLine("                </div>");
           sb.AppendLine("            </div>");
           sb.AppendLine("        </div>");
           return sb.ToString();
       }
       private class ProjectStats
       {
           public int SuccessCount;
           public int ErrorCount;
           public double SuccessRate;
           public double ErrorRate;
           public double MinSuccessTime = double.MaxValue;
           public double MaxSuccessTime;
           public double AvgSuccessTime;
           public double MinErrorTime = double.MaxValue;
           public double MaxErrorTime;
           public double AvgErrorTime;
           public int SuccessWithTime;
           public int ErrorWithTime;
       }
       private ProjectStats CalculateProjectStats(DailyReport.ProjectData proj, DateTime today, int maxAccountIndex)
       {
           var stats = new ProjectStats();
           double totalSuccessTime = 0;
           double totalErrorTime = 0;
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
                   if (isToday) stats.SuccessCount++;
                   if (!string.IsNullOrEmpty(timeStr) && double.TryParse(timeStr,
                           System.Globalization.NumberStyles.Any,
                           System.Globalization.CultureInfo.InvariantCulture, out double time) && time > 0)
                   {
                       totalSuccessTime += time;
                       stats.SuccessWithTime++;
                       if (time < stats.MinSuccessTime) stats.MinSuccessTime = time;
                       if (time > stats.MaxSuccessTime) stats.MaxSuccessTime = time;
                   }
               }
               else if (status == "-")
               {
                   if (isToday) stats.ErrorCount++;
                   if (!string.IsNullOrEmpty(timeStr) && double.TryParse(timeStr,
                           System.Globalization.NumberStyles.Any,
                           System.Globalization.CultureInfo.InvariantCulture, out double time) && time > 0)
                   {
                       totalErrorTime += time;
                       stats.ErrorWithTime++;
                       if (time < stats.MinErrorTime) stats.MinErrorTime = time;
                       if (time > stats.MaxErrorTime) stats.MaxErrorTime = time;
                   }
               }
           }
           stats.SuccessRate = maxAccountIndex > 0 ? (double)stats.SuccessCount / maxAccountIndex * 100 : 0;
           stats.ErrorRate = maxAccountIndex > 0 ? (double)stats.ErrorCount / maxAccountIndex * 100 : 0;
           stats.AvgSuccessTime = stats.SuccessWithTime > 0 ? totalSuccessTime / stats.SuccessWithTime : 0;
           stats.AvgErrorTime = stats.ErrorWithTime > 0 ? totalErrorTime / stats.ErrorWithTime : 0;
           if (stats.MinSuccessTime == double.MaxValue) stats.MinSuccessTime = 0;
           if (stats.MinErrorTime == double.MaxValue) stats.MinErrorTime = 0;
           return stats;
       }
       private string GenerateProjectStatsHtml(ProjectStats stats)
       {
           var sb = new StringBuilder();
           var statusClass = stats.SuccessRate >= 90 ? "stat-good" : (stats.SuccessRate >= 70 ? "" : "stat-bad");
           sb.AppendLine("                                    <div class='project-stats'>");
           sb.AppendLine("                                        <div class='stat-row'>");
           sb.AppendLine("                                            <span>✔️ Successful: </span>");
           sb.AppendLine($"                                            <span class='stat-good'>{stats.SuccessCount}</span>");
           sb.AppendLine("                                        </div>");
           if (stats.SuccessWithTime > 0)
           {
               sb.AppendLine("                                        <div class='stat-row'>");
               sb.AppendLine("                                            <span>Min|Max|Avg : </span>");
               sb.AppendLine($"                                            <span class='stat-neutral'>{stats.MinSuccessTime:F1}|{stats.MaxSuccessTime:F1}|{stats.AvgSuccessTime:F1}s</span>");
               sb.AppendLine("                                        </div>");
           }
           sb.AppendLine("                                        <div class='stat-row'>");
           sb.AppendLine("                                            <span>❌ Failed:  </span>");
           sb.AppendLine($"                                            <span class='stat-bad'>{stats.ErrorCount}</span>");
           sb.AppendLine("                                        </div>");
           if (stats.ErrorWithTime > 0)
           {
               sb.AppendLine("                                        <div class='stat-row'>");
               sb.AppendLine("                                            <span>Min|Max|Avg : </span>");
               sb.AppendLine($"                                            <span class='stat-neutral'>{stats.MinErrorTime:F1}|{stats.MaxErrorTime:F1}|{stats.AvgErrorTime:F1}s</span>");
               sb.AppendLine("                                        </div>");
           }
           sb.AppendLine("                                        <div class='stat-row'>");
           sb.AppendLine("                                            <span>[✔️/❌] Rate: </span>");
           sb.AppendLine($"                                            <span class='{statusClass}'>{stats.SuccessRate:F1}%</span>");
           sb.AppendLine("                                        </div>");
           sb.AppendLine("                                    </div>");
           return sb.ToString();
       }
       private string GenerateDailyCell(DailyReport.ProjectData proj, string accStr, DateTime today)
       {
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
               tooltipData = $"account #{accStr}||{proj.ProjectName}||{ts}||{completionTime}||" +
                             (status == "+" ? "success" : "error") + $"||{report}||daily";
           }
           else
           {
               tooltipData = $"account #{accStr}||{proj.ProjectName}||—||||notdone||||daily";
           }
           return $"<div class='{cellClass}' data-tooltip='{DailyReport.HtmlEncoder.HtmlAttributeEncode(tooltipData)}'></div>\n";
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
               /* Daily Report Cells */
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
                   max-width: calc((14px + 2px) * 100);
                }
                .heatmap-cell {
                   width: 14px;
                   height: 14px;
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
               /* Project Cards */
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
       /// <summary>
       /// Генерирует объединённый отчёт: Social Networks + Daily Report
       /// </summary>
       public static void GenerateUnionReport(
           this IZennoPosterProjectModel project,
           List<AccountSocialData> socialAccounts,
           List<DailyReport.ProjectData> dailyProjects,
           bool call = false)
       {
           new UnionReport(project, log: true).ShowUnionReport(socialAccounts, dailyProjects, call);
       }
       
       public static void GenerateFullUnionReport(this IZennoPosterProjectModel project)
       {
           // ===== ЧАСТЬ 1: Собираем данные Social Networks =====
           var socialAccounts = new List<AccountSocialData>();
           var acc0 = project.Int("rangeStart") - 1;
           while (acc0 < project.Int("rangeEnd"))
           {
               acc0++;
               project.Var("acc0", acc0);
               var account = new AccountSocialData(acc0);
               // Twitter
               var twitter = project.DbGetColumns("status, login", "_twitter", log: true);
               if (twitter.ContainsKey("login"))
               {
                   account.Twitter = new SocialStatus
                   {
                       Status = twitter["status"],
                       Login = twitter["login"]
                   };
               }
               // GitHub
               var github = project.DbGetColumns("status, login", "_github", log: true);
               if (github.ContainsKey("login"))
               {
                   account.GitHub = new SocialStatus
                   {
                       Status = github["status"],
                       Login = github["login"]
                   };
               }
               // Discord
               var discord = project.DbGetColumns("status, username", "_discord", log: true);
               if (discord.ContainsKey("username"))
               {
                   account.Discord = new SocialStatus
                   {
                       Status = discord["status"],
                       Login = discord["username"]
                   };
               }
               // Telegram
               var telegram = project.DbGetColumns("username", "_telegram", log: true);
               if (telegram.ContainsKey("username"))
               {
                   account.Telegram = new SocialStatus
                   {
                       Status = "ok",
                       Login = telegram["username"]
                   };
               }
               socialAccounts.Add(account);
           }
           // ===== ЧАСТЬ 2: Собираем данные Daily Projects =====
           var projectTables = project.TblList();
           var dailyProjects = new List<DailyReport.ProjectData>();
           foreach (var pj in projectTables)
           {
               if (!pj.StartsWith("__")) continue;
               if (pj.StartsWith("__|")) continue;
               var projectData = DailyReport.ProjectData.CollectData(project, pj);
               dailyProjects.Add(projectData);
           }
           // ===== ЧАСТЬ 3: Генерируем объединённый отчёт =====
           project.GenerateUnionReport(socialAccounts, dailyProjects, call: true);
       }

       
       
       
   }
   

}


namespace z3nCore.Examples

{   using z3nCore.Utilities;
    public static class UnionReportExample
   {

       public static void GenerateFullUnionReport(this IZennoPosterProjectModel project)
       {
           // ===== ЧАСТЬ 1: Собираем данные Social Networks =====
           var socialAccounts = new List<AccountSocialData>();
           var acc0 = project.Int("rangeStart") - 1;
           while (acc0 < project.Int("rangeEnd"))
           {
               acc0++;
               project.Var("acc0", acc0);
               var account = new AccountSocialData(acc0);
               // Twitter
               var twitter = project.DbGetColumns("status, login", "_twitter", log: true);
               if (twitter.ContainsKey("login"))
               {
                   account.Twitter = new SocialStatus
                   {
                       Status = twitter["status"],
                       Login = twitter["login"]
                   };
               }
               // GitHub
               var github = project.DbGetColumns("status, login", "_github", log: true);
               if (github.ContainsKey("login"))
               {
                   account.GitHub = new SocialStatus
                   {
                       Status = github["status"],
                       Login = github["login"]
                   };
               }
               // Discord
               var discord = project.DbGetColumns("status, username", "_discord", log: true);
               if (discord.ContainsKey("username"))
               {
                   account.Discord = new SocialStatus
                   {
                       Status = discord["status"],
                       Login = discord["username"]
                   };
               }
               // Telegram
               var telegram = project.DbGetColumns("username", "_telegram", log: true);
               if (telegram.ContainsKey("username"))
               {
                   account.Telegram = new SocialStatus
                   {
                       Status = "ok",
                       Login = telegram["username"]
                   };
               }
               socialAccounts.Add(account);
           }
           // ===== ЧАСТЬ 2: Собираем данные Daily Projects =====
           var projectTables = project.TblList();
           var dailyProjects = new List<DailyReport.ProjectData>();
           foreach (var pj in projectTables)
           {
               if (!pj.StartsWith("__")) continue;
               if (pj.StartsWith("__|")) continue;
               var projectData = DailyReport.ProjectData.CollectData(project, pj);
               dailyProjects.Add(projectData);
           }
           // ===== ЧАСТЬ 3: Генерируем объединённый отчёт =====
           project.GenerateUnionReport(socialAccounts, dailyProjects, call: true);
       }

       public static void GenerateUnionFromExistingData(
           IZennoPosterProjectModel project,
           List<AccountSocialData> socialAccounts,
           List<DailyReport.ProjectData> dailyProjects)
       {
           // Просто вызываем метод с готовыми данными
           project.GenerateUnionReport(socialAccounts, dailyProjects, call: true);
       }

       public static void GenerateUnionReportConditional(IZennoPosterProjectModel project)
       {
           var socialAccounts = CollectSocialData(project);
           var dailyProjects = CollectDailyData(project);
           // Проверяем что есть данные
           if (socialAccounts.Count == 0 && dailyProjects.Count == 0)
           {
               project.SendWarningToLog("No data to generate report");
               return;
           }
           // Генерируем отчёт
           project.GenerateUnionReport(socialAccounts, dailyProjects, call: true);
       }
       // Вспомогательные методы
       private static List<AccountSocialData> CollectSocialData(IZennoPosterProjectModel project)
       {
           var accounts = new List<AccountSocialData>();
           var acc0 = project.Int("rangeStart") - 1;
           while (acc0 < project.Int("rangeEnd"))
           {
               acc0++;
               project.Var("acc0", acc0);
               var account = new AccountSocialData(acc0);
               var twitter = project.DbGetColumns("status, login", "_twitter", log: true);
               if (twitter.ContainsKey("login"))
               {
                   account.Twitter = new SocialStatus
                   {
                       Status = twitter["status"],
                       Login = twitter["login"]
                   };
               }
               var github = project.DbGetColumns("status, login", "_github", log: true);
               if (github.ContainsKey("login"))
               {
                   account.GitHub = new SocialStatus
                   {
                       Status = github["status"],
                       Login = github["login"]
                   };
               }
               var discord = project.DbGetColumns("status, username", "_discord", log: true);
               if (discord.ContainsKey("username"))
               {
                   account.Discord = new SocialStatus
                   {
                       Status = discord["status"],
                       Login = discord["username"]
                   };
               }
               var telegram = project.DbGetColumns("username", "_telegram", log: true);
               if (telegram.ContainsKey("username"))
               {
                   account.Telegram = new SocialStatus
                   {
                       Status = "ok",
                       Login = telegram["username"]
                   };
               }
               accounts.Add(account);
           }
           return accounts;
       }
       private static List<DailyReport.ProjectData> CollectDailyData(IZennoPosterProjectModel project)
       {
           var projectTables = project.TblList();
           var projects = new List<DailyReport.ProjectData>();
           foreach (var pj in projectTables)
           {
               if (!pj.StartsWith("__")) continue;
               if (pj.StartsWith("__|")) continue;
               var projectData = DailyReport.ProjectData.CollectData(project, pj);
               projects.Add(projectData);
           }
           return projects;
       }
   }

}


