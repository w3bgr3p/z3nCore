using System;
using System.Collections.Generic;
using System.Text;
using ZennoLab.InterfacesLibrary.ProjectModel;
namespace z3nCore.Utilities

{
   /// <summary>
   /// Данные о социальных сетях для одного аккаунта
   /// </summary>
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
   public class SocialReport
   {
       private readonly IZennoPosterProjectModel _project;
       private readonly Logger _logger;
       private const char _r = '·';
       private const char _c = '¦';
       // Цвета социальных сетей
       private const string COLOR_TWITTER = "#1DA1F2";      // Голубой Twitter
       private const string COLOR_GITHUB = "#FFFFFF";        // Белый GitHub
       private const string COLOR_DISCORD = "#5865F2";       // Фиолетовый Discord
       private const string COLOR_TELEGRAM = "#0088CC";      // Синий Telegram
       public SocialReport(IZennoPosterProjectModel project, bool log = false)
       {
           _project = project;
           _logger = new Logger(project, log: log, classEmoji: "🌐");
       }
       /// <summary>
       /// Преобразует старый формат данных в новый (более эффективный)
       /// </summary>
       public static List<AccountSocialData> ConvertFromOldFormat(List<Dictionary<string, string[]>> dataList)
        {
           var result = new List<AccountSocialData>();
           int accountId = 1;
           foreach (var accountData in dataList)
           {
               var acc = new AccountSocialData(accountId);
               if (accountData.ContainsKey("twitter") && accountData["twitter"].Length >= 2)
               {
                   acc.Twitter = new SocialStatus
                   {
                       Status = accountData["twitter"][0],
                       Login = accountData["twitter"][1]
                   };
               }
               if (accountData.ContainsKey("github") && accountData["github"].Length >= 2)
               {
                   acc.GitHub = new SocialStatus
                   {
                       Status = accountData["github"][0],
                       Login = accountData["github"][1]
                   };
               }
               if (accountData.ContainsKey("discord") && accountData["discord"].Length >= 2)
               {
                   acc.Discord = new SocialStatus
                   {
                       Status = accountData["discord"][0],
                       Login = accountData["discord"][1]
                   };
               }
               if (accountData.ContainsKey("telegram") && accountData["telegram"].Length >= 1)
               {
                   acc.Telegram = new SocialStatus
                   {
                       Status = "ok", // Telegram не имеет статуса в исходных данных
                       Login = accountData["telegram"][0]
                   };
               }
               result.Add(acc);
               accountId++;
           }
           return result;
       }
       /// <summary>
       /// Генерирует HTML отчет с визуализацией социальных сетей
       /// </summary>
       public void ShowSocialTable(List<AccountSocialData> accounts, bool call = false)
       {
           _project.SendInfoToLog($"Generating social report for {accounts.Count} accounts...", false);
           string html = GenerateSocialHtml(accounts);
           string tempPath = System.IO.Path.Combine(_project.Path, ".data", "socialReport.html");
           System.IO.File.WriteAllText(tempPath, html, Encoding.UTF8);
           _project.SendInfoToLog($"Social report saved to: {tempPath}", false);
           if (call) System.Diagnostics.Process.Start(tempPath);
       }
       /// <summary>
       /// Генерирует HTML отчет из старого формата данных
       /// </summary>
       public void ShowSocialTableFromOldFormat(List<Dictionary<string, string[]>> dataList, bool call = false)
       {
           var accounts = ConvertFromOldFormat(dataList);
           ShowSocialTable(accounts, call);
       }
       private string GenerateSocialHtml(List<AccountSocialData> accounts)
       {
           var sb = new StringBuilder();
           var userId = _project.ExecuteMacro("{-Environment.CurrentUser-}");
           var title = $"Social Networks Report {DateTime.Now:dd.MM.yyyy [HH:mm:ss]} id: {userId}";
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
           // HTML Header
           sb.AppendLine("<!DOCTYPE html>");
           sb.AppendLine("<html lang='ru'>");
           sb.AppendLine("<head>");
           sb.AppendLine("    <meta charset='UTF-8'>");
           sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
           sb.AppendLine("    <title>" + title + "</title>");
           sb.AppendLine("    <style>");
           sb.AppendLine(GetStylesCss());
           sb.AppendLine("    </style>");
           sb.AppendLine("</head>");
           sb.AppendLine("<body>");
           sb.AppendLine("    <div id='tooltip' class='tooltip'></div>");
           sb.AppendLine("    <div class='container'>");
           // Header
           sb.AppendLine("        <div class='header'>");
           sb.AppendLine($"            <h1>🌐 {title}</h1>");
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
           sb.AppendLine($"                <div class='value' style='color: {COLOR_TWITTER};'>{totalTwitter}</div>");
           sb.AppendLine($"                <div class='subtext'>{activeTwitter} active</div>");
           sb.AppendLine("            </div>");
           sb.AppendLine("            <div class='summary-card'>");
           sb.AppendLine("                <h3>GITHUB</h3>");
           sb.AppendLine($"                <div class='value' style='color: {COLOR_GITHUB};'>{totalGitHub}</div>");
           sb.AppendLine($"                <div class='subtext'>{activeGitHub} active</div>");
           sb.AppendLine("            </div>");
           sb.AppendLine("            <div class='summary-card'>");
           sb.AppendLine("                <h3>DISCORD</h3>");
           sb.AppendLine($"                <div class='value' style='color: {COLOR_DISCORD};'>{totalDiscord}</div>");
           sb.AppendLine($"                <div class='subtext'>{activeDiscord} active</div>");
           sb.AppendLine("            </div>");
           sb.AppendLine("            <div class='summary-card'>");
           sb.AppendLine("                <h3>TELEGRAM</h3>");
           sb.AppendLine($"                <div class='value' style='color: {COLOR_TELEGRAM};'>{totalTelegram}</div>");
           sb.AppendLine($"                <div class='subtext'>{activeTelegram} active</div>");
           sb.AppendLine("            </div>");
           sb.AppendLine("        </div>");
           // Heatmap Section
           sb.AppendLine("        <div class='section'>");
           sb.AppendLine("            <h2>🗺️ Social Networks HeatMap</h2>");
           sb.AppendLine("            <div class='heatmap-container'>");
           sb.AppendLine("                <div class='heatmap-wrapper'>");
           // Legend
           sb.AppendLine("                    <div class='heatmap-legend'>");
           sb.AppendLine("                        <span>Legend:</span>");
           sb.AppendLine($"                        <div class='legend-item'><div class='legend-box' style='background: {COLOR_TWITTER};'></div> Twitter</div>");
           sb.AppendLine($"                        <div class='legend-item'><div class='legend-box' style='background: {COLOR_GITHUB}; border-color: #666;'></div> GitHub</div>");
           sb.AppendLine($"                        <div class='legend-item'><div class='legend-box' style='background: {COLOR_DISCORD};'></div> Discord</div>");
           sb.AppendLine($"                        <div class='legend-item'><div class='legend-box' style='background: {COLOR_TELEGRAM};'></div> Telegram</div>");
           sb.AppendLine("                        <div class='legend-item'><div class='legend-box' style='opacity: 0.3; background: #666;'></div> Not OK</div>");
           sb.AppendLine("                    </div>");
           // Heatmap cells
           sb.AppendLine("                    <div class='cells-grid'>");
            foreach (var acc in accounts)
            {
                sb.AppendLine("                        <div class='account-cell'>");
                sb.AppendLine("                            <div class='social-squares'>");

               // Twitter (верхний левый)
               sb.Append(GenerateSocialSquare("twitter", acc.Twitter, acc.AccountId));
               // GitHub (верхний правый)
               sb.Append(GenerateSocialSquare("github", acc.GitHub, acc.AccountId));
               // Discord (нижний левый)
               sb.Append(GenerateSocialSquare("discord", acc.Discord, acc.AccountId));
               // Telegram (нижний правый)
               sb.Append(GenerateSocialSquare("telegram", acc.Telegram, acc.AccountId));
               sb.AppendLine("                            </div>");
               sb.AppendLine("                        </div>");
           }
           sb.AppendLine("                    </div>");
           sb.AppendLine("                </div>");
           sb.AppendLine("            </div>");
           sb.AppendLine("        </div>");
           sb.AppendLine("    </div>");
           // JavaScript
           sb.AppendLine("    <script>");
           sb.AppendLine(GetJavaScript());
           sb.AppendLine("    </script>");
           sb.AppendLine("</body>");
           sb.AppendLine("</html>");
           return sb.ToString();
       }
       private string GenerateSocialSquare(string socialName, SocialStatus social, int accountId)
       {
           string color;
            switch (socialName)
            {
                case "twitter":
                    color = COLOR_TWITTER;
                    break;
                case "github":
                    color = COLOR_GITHUB;
                    break;
                case "discord":
                    color = COLOR_DISCORD;
                    break;
                case "telegram":
                    color = COLOR_TELEGRAM;
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
               tooltipData = $"account #{accountId}||{socialName}||{social.Login}||{status}";
           }
           else
           {
               tooltipData = $"account #{accountId}||{socialName}||||not connected";
           }
           return $"<div class='social-square' style='{background}{opacity}' " +
                  $"data-social='{socialName}' " +
                  $"data-tooltip='{DailyReport.HtmlEncoder.HtmlAttributeEncode(tooltipData)}'></div>\n";
       }
       private string GetStylesCss()
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
                .cells-grid {
                    display: flex;
                    flex-wrap: wrap;
                    gap: 2px;
                    max-width:   100%;  /* calc((11px + 2px) * 100);   */
                }
                .account-cell {
                    width: calc((100vw - 60px) / 100 - 2px);
                    height: calc((100vw - 60px) / 100 - 2px);
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
                   max-width: 300px;
                   line-height: 1.5;
               }
               .tooltip.show { display: block; }
               .tooltip-title {
                   font-weight: 600;
                   margin-bottom: 6px;
                   color: #58a6ff;
                   font-size: 13px;
               }
               .tooltip-social {
                   color: #8b949e;
                   font-size: 11px;
                   margin-bottom: 6px;
                   text-transform: capitalize;
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
                   font-size: 11px;
                   margin-top: 6px;
               }
               .tooltip-status.ok { color: #3fb950; }
               .tooltip-status.error { color: #f85149; }
               .tooltip-status.empty { color: #8b949e; }
               @media (max-width: 768px) {
                   .summary-cards { flex-direction: column; }
                   .cells-grid { justify-content: center; }
               }
           ";
       }
       private string GetJavaScript()
       {
           return @"
               const tooltip = document.getElementById('tooltip');
               const socialSquares = document.querySelectorAll('.social-square');
               socialSquares.forEach(square => {
                   square.addEventListener('mouseenter', function(e) {
                       const data = this.getAttribute('data-tooltip');
                       if (!data) return;
                       const parts = data.split('||');
                       const account = parts[0];
                       const social = parts[1];
                       const login = parts[2];
                       const status = parts[3];
                       let content = '<div class=""tooltip-title"">' + account + '</div>';
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
                   square.addEventListener('mouseleave', function() {
                       tooltip.classList.remove('show');
                   });
                   square.addEventListener('click', function(e) {
                       const data = this.getAttribute('data-tooltip');
                       if (!data) return;
                       const parts = data.split('||');
                       const account = parts[0];
                       const social = parts[1];
                       const login = parts[2];
                       const status = parts[3];
                       let copyText = account + '\n' + social;
                       if (login && login !== '') {
                           copyText += '\n' + login;
                           copyText += '\nStatus: ' + status;
                       } else {
                           copyText += '\nNot connected';
                       }
                       navigator.clipboard.writeText(copyText).then(function() {
                           const originalBorder = square.style.border;
                           square.style.border = '2px solid #58a6ff';
                           setTimeout(function() {
                               square.style.border = originalBorder;
                           }, 300);
                       }).catch(function(err) {
                           console.error('Copy error:', err);
                       });
                   });
               });
           ";
       }
   }

}

namespace z3nCore

{
   using Utilities;
   public static partial class ProjectExtensions
   {
       /// <summary>
       /// Генерирует HTML отчет о социальных сетях из нового формата данных
       /// </summary>
       public static void GenerateSocialReport(this IZennoPosterProjectModel project,
           List<AccountSocialData> accounts, bool call = false)
       {
           new SocialReport(project, log: true).ShowSocialTable(accounts, call);
       }
       /// <summary>
       /// Генерирует HTML отчет о социальных сетях из старого формата данных
       /// </summary>
       public static void GenerateSocialReportFromOldFormat(this IZennoPosterProjectModel project,
           List<Dictionary<string, string[]>> dataList, bool call = false)
       {
           new SocialReport(project, log: true).ShowSocialTableFromOldFormat(dataList, call);
       }
   }

}
