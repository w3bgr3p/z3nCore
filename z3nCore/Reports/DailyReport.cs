using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZennoLab.InterfacesLibrary.ProjectModel;


namespace z3nCore.Utilities
{
public class DailyReport
    {
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
                        All.Add(acc, new string[] { completionStatus, ts, completionSec, report });
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

        public class FarmReportGenerator
        {
            public static string GenerateHtmlReport(List<ProjectData> projects, DateTime reportDate, string userId = null)
            {
                var html = new StringBuilder();

                // Собираем информацию о процессах
                List<string[]> zennoProcesses = new List<string[]>();
                try
                {
                    var zp = Utilities.Debugger.ZennoProcesses();
                    foreach (string[] arr in zp)
                    {
                        zennoProcesses.Add(arr);
                    }
                }
                catch
                {
                }

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

                var title = $"Report {userId} {reportDate.ToString("dd.MM.yyyy [HH:mm:ss]")}";
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html lang='ru'>");
                html.AppendLine("<head>");
                html.AppendLine("    <meta charset='UTF-8'>");
                html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
                html.AppendLine("    <title>" + title + "</title>");
                html.AppendLine("    <style>");
                html.AppendLine(@"
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body { 
                    font-family: 'Iosevka', 'Consolas', monospace;
                    background: #0d1117;
                    padding: 15px;
                    color: #c9d1d9;
                }
                .container { max-width: 1900px; margin: 0 auto; }
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
                .header .date { color: #8b949e; font-size: 12px; }
                
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
                    margin-bottom: 10px;
                    font-size: 11px;
                    color: #8b949e;
                }
                .legend-item {
                    display: flex;
                    align-items: center;
                    gap: 5px;
                }
                .legend-box {
                    width: 10px;
                    height: 10px;
                    border-radius: 2px;
                    border: 1px solid #30363d;
                }
                .legend-box.success { background: #238636; }
                .legend-box.error { background: #da3633; }
                .legend-box.success-old { background: #1a4221; }
                .legend-box.error-old { background: #5e2322; }
                .legend-box.notdone { background: transparent; }
                
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
                .stats-sidebar {
                    flex: 1;  /* ← растянется на свободное место */
                    max-width: 250px;  /* ← ограничит максимум, можно убрать если хочешь до конца */
                    flex-shrink: 0;
                }
                .stats-card {
                    background: #0d1117;
                    border: 1px solid #30363d;
                    border-radius: 6px;
                    padding: 8px;
                    position: sticky;
                    top: 15px;
                }
                .stats-card h3 {
                    color: #c9d1d9;
                    font-size: 10px;
                    font-weight: 600;
                    margin-bottom: 6px;
                    padding-bottom: 4px;
                    border-bottom: 1px solid #30363d;
                }
                .processes-list {
                    font-size: 9px;
                    color: #8b949e;
                    line-height: 1.6;
                }
                .process-line {
                    display: flex;
                    gap: 8px;
                    white-space: nowrap;
                    overflow: hidden;
                    text-overflow: ellipsis;
                }
                .process-name {
                    flex: 1;  /* растянется и займет оставшееся место */
                    color: #58a6ff;
                    overflow: hidden;
                    text-overflow: ellipsis;
                }
                .process-mem {
                    min-width: 40px;  /* фиксированная ширина для выравнивания */
                    text-align: right;
                    color: #3fb950;
                }
                .process-time {
                    min-width: 50px;
                    text-align: right;
                    color: #8b949e;
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
                .heatmap-cell.success-old { background: #1a4221; border-color: #2c5832; }
                .heatmap-cell.error-old { background: #5e2322; border-color: #723332; }
                .heatmap-cell:hover {
                    transform: scale(1.3);
                    z-index: 10;
                    box-shadow: 0 0 8px rgba(255,255,255,0.3);
                }
                
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
                .tooltip-time {
                    color: #8b949e;
                    font-size: 11px;
                    margin-bottom: 8px;
                }
                .tooltip-status {
                    margin: 5px 0;
                    padding: 4px 8px;
                    border-radius: 4px;
                    display: inline-block;
                }
                .tooltip-status.success { 
                    color: #3fb950;
                    background: rgba(63, 185, 80, 0.1);
                }
                .tooltip-status.error { 
                    color: #f85149;
                    background: rgba(248, 81, 73, 0.1);
                }
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
                
                @media (max-width: 768px) {
                    .project-grid { grid-template-columns: 1fr; }
                    .summary-cards { grid-template-columns: 1fr; }
                    .heatmap-with-stats { flex-direction: column; }
                    .heatmap-project-card { width: 100%; max-width: 320px; }
                    .heatmap-grid { flex-direction: column; }
                    .stats-sidebar { width: 100%; }
                }
            ");
                html.AppendLine("    </style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                html.AppendLine("    <div id='tooltip' class='tooltip'></div>");
                html.AppendLine("    <div class='container'>");

                html.AppendLine("        <div class='header'>");
                html.AppendLine($"            <h4>📊 {title}</h4>");
                //html.AppendLine("            <div class='date'>Дата: " + reportDate.ToString("dd MMMM yyyy") + "</div>");
                html.AppendLine("        </div>");

                html.AppendLine("        <div class='summary-cards'>");
                html.AppendLine("            <div class='summary-card'>");
                html.AppendLine("                <h3>TOTAL ATTEMPTS</h3>");
                html.AppendLine("                <div class='value'>" + totalAccounts + "</div>");
                html.AppendLine("                <div class='subtext'>In all projects</div>");
                html.AppendLine("            </div>");
                html.AppendLine("            <div class='summary-card success'>");
                html.AppendLine("                <h3>DONE</h3>");
                html.AppendLine("                <div class='value'>" + totalSuccess + "</div>");
                html.AppendLine("                <div class='subtext'>" + overallSuccessRate.ToString("F1") +
                                "% succsess</div>");
                html.AppendLine("            </div>");
                html.AppendLine("            <div class='summary-card error'>");
                html.AppendLine("                <h3>FAILED</h3>");
                html.AppendLine("                <div class='value'>" + totalErrors + "</div>");
                html.AppendLine("                <div class='subtext'>! Needs attention</div>");
                html.AppendLine("            </div>");
                html.AppendLine("        </div>");

                html.AppendLine("        <div class='section'>");
                html.AppendLine("            <h2> HeatMap</h2>");
                html.AppendLine("            <div class='heatmap-container'>");
                html.AppendLine("                <div class='heatmap-wrapper'>");

                html.AppendLine("                    <div class='heatmap-legend'>");
                html.AppendLine("                        <span>Legend:</span>");
                html.AppendLine("                        <div class='legend-item'><div class='legend-box success'></div> Today's Success</div>");
                html.AppendLine("                        <div class='legend-item'><div class='legend-box error'></div> Today's Error</div>");
                html.AppendLine("                        <div class='legend-item'><div class='legend-box success-old'></div> Old Success</div>");
                html.AppendLine("                        <div class='legend-item'><div class='legend-box error-old'></div> Old Error</div>");
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
                        var timeStr = data[2].Trim();

                        if (status == "+")
                        {
                            successCount++;
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
                            errorCount++;
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
                    html.AppendLine("                                    <div class='project-name'>" +
                                    proj.ProjectName + "</div>");
                    html.AppendLine("                                    <div class='progress-bar'>");
                    html.AppendLine(
                        "                                        <div style='display: flex; height: 100%; width: 100%;'>");
                    html.AppendLine("                                            <div style='width: " +
                                    successRate.ToString("F1") + "%; background: #238636;'></div>");
                    html.AppendLine("                                            <div style='width: " +
                                    errorRate.ToString("F1") + "%; background: #da3633;'></div>");
                    html.AppendLine("                                        </div>");
                    html.AppendLine("                                    </div>");
                    
                    html.AppendLine("                                    <div class='project-stats'>");
                    html.AppendLine("                                        <div class='stat-row'>");
                    html.AppendLine("                                            <span>✔️ Successful: </span>");
                    html.AppendLine("                                            <span class='stat-good'>" +
                                    successCount + "</span>");
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
                    html.AppendLine("                                            <span class='stat-bad'>" + errorCount +
                                    "</span>");
                    html.AppendLine("                                        </div>");
                    
                    //time err
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
                    
                    //succsess rate
                    html.AppendLine("                                        <div class='stat-row'>");
                    html.AppendLine("                                            <span>[✔️/❌] Rate: </span>");
                    html.AppendLine("                                            <span class='" + statusClass + "'>" +
                                    successRate.ToString("F1") + "%</span>");
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

                            // START === Изменения для бледных цветов ===
                            bool isOld = false;
                            if (DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime timestamp))
                            {
                                isOld = timestamp.Date < reportDate.Date;
                            }

                            if (status == "+")
                            {
                                cellClass += isOld ? " success-old" : " success";
                            }
                            else if (status == "-")
                            {
                                cellClass += isOld ? " error-old" : " error";
                            }
                            // END ===== Изменения для бледных цветов =====

                            tooltipData = "Аккаунт #" + accStr + "||" +
                                          proj.ProjectName + "||" +
                                          ts + "||" +
                                          completionTime + "||" +
                                          (status == "+" ? "success" : "error") + "||" +
                                          report;
                        }
                        else
                        {
                            tooltipData = "Аккаунт #" + accStr + "||" +
                                          proj.ProjectName + "||" +
                                          "—||" +
                                          "||" +
                                          "notdone||";
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

                // Stats sidebar
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
                    html.AppendLine(
                        "                                <div style='color: #8b949e; font-size: 8px;'>Нет данных</div>");
                }

                html.AppendLine("                            </div>");
                html.AppendLine("                        </div>");
                html.AppendLine("                    </div>");
                html.AppendLine("                </div>");
                html.AppendLine("            </div>");
                html.AppendLine("        </div>");

                var emptyProjects = projects.Where(p => p.All.Count == 0).ToList();
                if (emptyProjects.Count > 0)
                {
                    html.AppendLine("        <div class='section'>");
                    html.AppendLine("            <h2>📁 Проекты без активности</h2>");
                    html.AppendLine("            <div class='project-grid'>");

                    foreach (var project in emptyProjects)
                    {
                        html.AppendLine("                <div class='project-card'>");
                        html.AppendLine("                    <div class='project-name'>" + project.ProjectName +
                                        "</div>");
                        html.AppendLine(
                            "                    <div style='color: #8b949e; font-size: 12px;'>Нет данных за сегодня</div>");
                        html.AppendLine("                </div>");
                    }

                    html.AppendLine("            </div>");
                    html.AppendLine("        </div>");
                }

                html.AppendLine("    </div>");

                html.AppendLine("    <script>");
                html.AppendLine(@"
                const tooltip = document.getElementById('tooltip');
                const cells = document.querySelectorAll('.heatmap-cell');
                
                cells.forEach(cell => {
                    cell.addEventListener('mouseenter', function(e) {
                        const data = this.getAttribute('data-tooltip');
                        if (!data) return;
                        
                        const parts = data.split('||');
                        const acc = parts[0];
                        const project = parts[1];
                        const time = parts[2];
                        const completionTime = parts[3];
                        const status = parts[4];
                        const report = parts[5] || '';
                        
                        let content = '<div class=""tooltip-title"">' + acc + '</div>';
                        content += '<div style=""color: #8b949e; margin-bottom: 5px;"">' + project + '</div>';
                        
                        if (time !== '—') {
                            content += '<div class=""tooltip-time"">⏱ ' + time;
                            if (completionTime && completionTime !== '') {
                                content += ' (' + completionTime + 's)';
                            }
                            content += '</div>';
                        }
                        
                        if (status === 'success') {
                            content += '<div class=""tooltip-status success"">✓ Успешно</div>';
                        } else if (status === 'error') {
                            content += '<div class=""tooltip-status error"">✗ Ошибка</div>';
                        } else {
                            content += '<div style=""color: #8b949e; font-size: 11px;"">Не выполнено</div>';
                        }
                        
                        if (report && report.trim() !== '') {
                            var reportClass = status === 'error' ? 'tooltip-error' : 'tooltip-info';
                            content += '<div class=""' + reportClass + '"">' + report.replace(/\n/g, '<br>') + '</div>';
                        }
                        
                        tooltip.innerHTML = content;
                        tooltip.classList.add('show');
                        
                        const rect = this.getBoundingClientRect();
                        const tooltipRect = tooltip.getBoundingClientRect();
                        
                        let left = rect.left + window.scrollX - tooltipRect.width / 2 + rect.width / 2;
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
                        const acc = parts[0];
                        const project = parts[1];
                        const time = parts[2];
                        const completionTime = parts[3];
                        const status = parts[4];
                        const report = parts[5] || '';
                        
                        let copyText = acc + '\n' + project;
                        if (time !== '—') {
                            copyText += '\n' + time;
                            if (completionTime && completionTime !== '') {
                                copyText += ' (' + completionTime + 's)';
                            }
                        }
                        
                        if (status === 'success') {
                            copyText += '\nСтатус: Успешно';
                        } else if (status === 'error') {
                            copyText += '\nСтатус: Ошибка';
                            if (report && report.trim() !== '') {
                                copyText += '\n\nОшибка:\n' + report;
                            }
                        } else {
                            copyText += '\nСтатус: Не выполнено';
                        }
                        
                        navigator.clipboard.writeText(copyText).then(function() {
                            const originalBorder = cell.style.border;
                            cell.style.border = '2px solid #58a6ff';
                            setTimeout(function() {
                                cell.style.border = originalBorder;
                            }, 300);
                        }).catch(function(err) {
                            console.error('Ошибка копирования:', err);
                        });
                    });
                });
                ");
                html.AppendLine("    </script>");
                html.AppendLine("</body>");
                html.AppendLine("</html>");

                return html.ToString();
            }
        }
    }


    public static partial class ProjectExtensions
    {
        public static void ReportDailyHtml(this IZennoPosterProjectModel project, bool call = false)
        {
            string user = project.ExecuteMacro("{-Environment.CurrentUser-}");
            
            var projectTables = project.DbQ(@"SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            ORDER BY table_name;").Split('·').ToList();


            var projects = new List<DailyReport.ProjectData>();

            foreach (var pj in projectTables)
            {
                if (!pj.StartsWith("__")) continue;
                if (pj.StartsWith("__|")) continue;
                var projectData = DailyReport.ProjectData.CollectData(project, pj);
                projects.Add(projectData);
            }

            var html = DailyReport.FarmReportGenerator.GenerateHtmlReport(projects, DateTime.Now, user);
            string tempPath = System.IO.Path.Combine(project.Path, ".data", "dailyReport.html");
            System.IO.File.WriteAllText(tempPath, html, System.Text.Encoding.UTF8);
            project.SendInfoToLog($"Report saved to: {tempPath}", false);
            if (call) System.Diagnostics.Process.Start(tempPath);
        }
    }

}