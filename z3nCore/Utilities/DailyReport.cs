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
            public Dictionary<string, string> Positive { get; set; } // acc -> timestamp
            public Dictionary<string, string[]> Negative { get; set; } // acc -> [timestamp, report]
            public Dictionary<string, string[]> All { get; set; } // acc -> [timestamp, report]
            
            public static ProjectData CollectData(IZennoPosterProjectModel project, string tableName)
            {
                project.Var("projectTable", tableName.Trim());
                char _c = '¦';

                var rawPositive = project.DbGetLines("id, last", where: "last like '+ %'");
                var rawNegative = project.DbGetLines("id, last", where: "last like '- %'");

                var Negative = new Dictionary<string, string[]>();
                var Positive = new Dictionary<string, string>();
                var All = new Dictionary<string, string[]>();

                // Обработка негативных результатов
                foreach (var str in rawNegative) 
                {
                    if (string.IsNullOrWhiteSpace(str)) continue; // Пропускаем пустые строки
                    
                    var lines = str.Split('\n');
                    if (lines.Length == 0) continue;
                    
                    var firstLine = lines[0];
                    if (string.IsNullOrWhiteSpace(firstLine)) continue;
                    
                    var parts = firstLine.Split(' ');
                    if (parts.Length < 2) continue;
                    
                    var acc = str.Split(_c)[0];
                    var ts = parts[1];
                    
                    // Безопасное удаление первой строки
                    var report = "";
                    if (lines.Length > 1)
                    {
                        report = string.Join("\n", lines.Skip(1)).Trim();
                    }
                    
                    if (!Negative.ContainsKey(acc))
                    {
                        Negative.Add(acc, new string[] { ts, report });
                    }
                    
                    if (!All.ContainsKey(acc))
                    {
                        All.Add(acc, new string[] { ts, report });
                    }
                }

                // Обработка позитивных результатов
                foreach (var str in rawPositive) 
                {
                    if (string.IsNullOrWhiteSpace(str)) continue; // Пропускаем пустые строки
                    
                    var lines = str.Split('\n');
                    if (lines.Length == 0) continue;
                    
                    var firstLine = lines[0];
                    if (string.IsNullOrWhiteSpace(firstLine)) continue;
                    
                    var parts = firstLine.Split(' ');
                    if (parts.Length < 2) continue;
                    
                    var acc = str.Split(_c)[0];
                    var ts = parts[1];
                    
                    if (!Positive.ContainsKey(acc))
                    {
                        Positive.Add(acc, ts);
                    }

                    // Если аккаунт уже есть в All (был в Negative), не перезаписываем
                    if (!All.ContainsKey(acc))
                    {
                        All.Add(acc, new string[] { ts, "" });
                    }
                }

                // ВОЗВРАЩАЕМ данные проекта
                return new ProjectData
                {
                    ProjectName = tableName.Replace("__", ""),
                    Positive = Positive,
                    Negative = Negative,
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
                public static string GenerateHtmlReport(List<ProjectData> projects, DateTime reportDate)
            {
                var html = new StringBuilder();

                // Вычисляем общую статистику
                int totalAccounts = 0;
                int totalSuccess = 0;
                int totalErrors = 0;

                foreach (var project in projects)
                {
                    totalAccounts += project.All.Count;
                    totalSuccess += project.Positive.Count;
                    totalErrors += project.Negative.Count;
                }

                var overallSuccessRate = totalAccounts > 0 ? (double)totalSuccess / totalAccounts * 100 : 0;

                // Определяем максимальный индекс аккаунта
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

                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html lang='ru'>");
                html.AppendLine("<head>");
                html.AppendLine("    <meta charset='UTF-8'>");
                html.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
                html.AppendLine("    <title>Отчёт по ферме аккаунтов - " + reportDate.ToString("dd.MM.yyyy") +
                                "</title>");
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
                
                /* GitHub-style Heatmap */
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
                    gap: 10px;
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
                .legend-box.notdone { background: transparent; }
                
                .heatmap-grid {
                    display: grid;
                    gap: 15px;
                }
                .heatmap-with-stats {
                    display: flex;
                    gap: 12px;
                    align-items: stretch;  // ← растягивает по высоте
                }
                .heatmap-content {
                    flex: 1;
                    min-width: 0;
                }
                .heatmap-project-card {
                    width: 150px;
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
                .heatmap-cell.success { 
                    background: #238636;
                    border-color: #238636;
                }
                .heatmap-cell.error { 
                    background: #da3633;
                    border-color: #da3633;
                }
                .heatmap-cell:hover {
                    transform: scale(1.3);
                    z-index: 10;
                    box-shadow: 0 0 8px rgba(255,255,255,0.3);
                }
                
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
                .progress-fill {
                    height: 100%;
                    background: #238636;
                    transition: width 0.5s;
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
                
                @media (max-width: 768px) {
                    .project-grid { grid-template-columns: 1fr; }
                    .summary-cards { grid-template-columns: 1fr; }
                    .heatmap-with-stats { flex-direction: column; }
                    .heatmap-project-card { width: 100%; max-width: 320px; }
                }
                ");
                html.AppendLine("    </style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                // Tooltip element
                html.AppendLine("    <div id='tooltip' class='tooltip'></div>");

                html.AppendLine("    <div class='container'>");

                // Header
                html.AppendLine("        <div class='header'>");
                html.AppendLine("            <h1>📊 Отчёт по ферме аккаунтов</h1>");
                html.AppendLine("            <div class='date'>Дата: " + reportDate.ToString("dd MMMM yyyy") +
                                "</div>");
                html.AppendLine("        </div>");

                // Summary Cards
                html.AppendLine("        <div class='summary-cards'>");
                html.AppendLine("            <div class='summary-card'>");
                html.AppendLine("                <h3>ВСЕГО ЗАДАЧ</h3>");
                html.AppendLine("                <div class='value'>" + totalAccounts + "</div>");
                html.AppendLine("                <div class='subtext'>По всем проектам</div>");
                html.AppendLine("            </div>");
                html.AppendLine("            <div class='summary-card success'>");
                html.AppendLine("                <h3>УСПЕШНО</h3>");
                html.AppendLine("                <div class='value'>" + totalSuccess + "</div>");
                html.AppendLine("                <div class='subtext'>" + overallSuccessRate.ToString("F1") +
                                "% успешных</div>");
                html.AppendLine("            </div>");
                html.AppendLine("            <div class='summary-card error'>");
                html.AppendLine("                <h3>ОШИБКИ</h3>");
                html.AppendLine("                <div class='value'>" + totalErrors + "</div>");
                html.AppendLine("                <div class='subtext'>Требуют внимания</div>");
                html.AppendLine("            </div>");
                html.AppendLine("        </div>");

                // GitHub-style Heatmap Section с карточками
                html.AppendLine("        <div class='section'>");
                html.AppendLine("            <h2>🔥 Карта активности аккаунтов</h2>");
                html.AppendLine("            <div class='heatmap-container'>");
                html.AppendLine("                <div class='heatmap-wrapper'>");

                // Legend
                html.AppendLine("                    <div class='heatmap-legend'>");
                html.AppendLine("                        <span>Статус:</span>");
                html.AppendLine(
                    "                        <div class='legend-item'><div class='legend-box success'></div> Успех</div>");
                html.AppendLine(
                    "                        <div class='legend-item'><div class='legend-box error'></div> Ошибка</div>");
                html.AppendLine(
                    "                        <div class='legend-item'><div class='legend-box notdone'></div> Не выполнено</div>");
                html.AppendLine("                    </div>");

                // Heatmap Grid
                html.AppendLine("                    <div class='heatmap-grid'>");

                foreach (var project in projects)
                {
                    if (project.All.Count == 0) continue;

                    var successCount = project.Positive.Count;
                    var errorCount = project.Negative.Count;
                    var total = project.All.Count;
                    var successRate = total > 0 ? (double)successCount / maxAccountIndex * 100 : 0;
                    var errorRate = total > 0 ? (double)errorCount / maxAccountIndex * 100 : 0;
                    var statusClass = successRate >= 90 ? "stat-good" : (successRate >= 70 ? "" : "stat-bad");

                    html.AppendLine("                        <div class='heatmap-with-stats'>");

                    // Карточка статистики СЛЕВА от heatmap
                    html.AppendLine("                            <div class='heatmap-project-card'>");
                    html.AppendLine("                                <div class='project-card'>");
                    html.AppendLine("                                    <div class='project-name'>" +
                                    project.ProjectName + "</div>");
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
                    html.AppendLine("                                            <span>Успешно: </span>");
                    html.AppendLine("                                            <span class='stat-good'>" +
                                    successCount + "</span>");
                    html.AppendLine("                                        </div>");
                    html.AppendLine("                                        <div class='stat-row'>");
                    html.AppendLine("                                            <span>Ошибки:  </span>");
                    html.AppendLine("                                            <span class='stat-bad'>" + errorCount +
                                    "</span>");
                    html.AppendLine("                                        </div>");
                    html.AppendLine("                                        <div class='stat-row'>");
                    html.AppendLine("                                            <span>Процент: </span>");
                    html.AppendLine("                                            <span class='" + statusClass + "'>" +
                                    successRate.ToString("F1") + "%</span>");
                    html.AppendLine("                                        </div>");
                    html.AppendLine("                                    </div>");
                    html.AppendLine("                                </div>");
                    html.AppendLine("                            </div>");

                    // Heatmap СПРАВА от карточки (БЕЗ label)
                    html.AppendLine("                            <div class='heatmap-content'>");
                    html.AppendLine("                                <div class='heatmap-row'>");
                    html.AppendLine("                                    <div class='cells-container'>");

                    for (int i = 1; i <= maxAccountIndex; i++)
                    {
                        var accStr = i.ToString();
                        var cellClass = "heatmap-cell";
                        var tooltipData = "";

                        if (project.Negative.ContainsKey(accStr))
                        {
                            cellClass += " error";
                            var ts = project.Negative[accStr][0];
                            var report = project.Negative[accStr][1];

                            tooltipData = "Аккаунт #" + accStr + "||" +
                                          project.ProjectName + "||" +
                                          ts + "||" +
                                          "error||" +
                                          report;
                        }
                        else if (project.Positive.ContainsKey(accStr))
                        {
                            cellClass += " success";
                            var ts = project.Positive[accStr];

                            tooltipData = "Аккаунт #" + accStr + "||" +
                                          project.ProjectName + "||" +
                                          ts + "||" +
                                          "success||";
                        }
                        else
                        {
                            tooltipData = "Аккаунт #" + accStr + "||" +
                                          project.ProjectName + "||" +
                                          "—||" +
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

                html.AppendLine("                    </div>");
                html.AppendLine("                </div>");
                html.AppendLine("            </div>");
                html.AppendLine("        </div>");

                // Projects Section with Stats (только для пустых проектов)
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

                // JavaScript for tooltip
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
                        const status = parts[3];
                        const report = parts[4] || '';
                        
                        let content = '<div class=""tooltip-title"">' + acc + '</div>';
                        content += '<div style=""color: #8b949e; margin-bottom: 5px;"">' + project + '</div>';
                        
                        if (time !== '—') {
                            content += '<div class=""tooltip-time"">⏱ ' + time + '</div>';
                        }
                        
                        if (status === 'success') {
                            content += '<div class=""tooltip-status success"">✓ Успешно</div>';
                        } else if (status === 'error') {
                            content += '<div class=""tooltip-status error"">✗ Ошибка</div>';
                        } else {
                            content += '<div style=""color: #8b949e; font-size: 11px;"">Не выполнено</div>';
                        }
                        
                        if (report && report.trim() !== '') {
                            content += '<div class=""tooltip-error"">' + report.replace(/\n/g, '<br>') + '</div>';
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
                        const status = parts[3];
                        const report = parts[4] || '';
                        
                        let copyText = acc + '\n' + project + '\n' + time;
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
        public static void DalilyReport(this IZennoPosterProjectModel project, bool call = false)
        {
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

            var html = DailyReport.FarmReportGenerator.GenerateHtmlReport(projects, DateTime.Now);
            string tempPath = System.IO.Path.Combine(project.Path, ".data", "dailyReport.html");
            System.IO.File.WriteAllText(tempPath, html, System.Text.Encoding.UTF8);
            project.SendInfoToLog($"Report saved to: {tempPath}", false);
            if (call) System.Diagnostics.Process.Start(tempPath);
        }
    }

}