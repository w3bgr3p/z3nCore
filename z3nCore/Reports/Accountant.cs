
using System;
using System.Collections.Generic;

using System.Linq;

using ZennoLab.InterfacesLibrary.ProjectModel;



namespace z3nCore.Utilities
{
    public class Accountant
    {
        #region Constants
        
        // Пороги для цветовой индикации
        private const decimal BALANCE_THRESHOLD_HIGHEST = 0.1m;      // Синий
        private const decimal BALANCE_THRESHOLD_HIGH = 0.01m;        // Зеленый
        private const decimal BALANCE_THRESHOLD_MEDIUM = 0.001m;     // Желто-зеленый
        private const decimal BALANCE_THRESHOLD_LOW = 0.0001m;       // Хаки
        private const decimal BALANCE_THRESHOLD_VERYLOW = 0.00001m;  // Лососевый
        // > 0 = Красный, = 0 = Белый на белом
        
        private const string BALANCE_FORMAT = "0.0000000";
        private const int ROWS_PER_PAGE = 50;  // Количество строк на одной странице
        private const char _r = '·';
        private const char _c = '¦';
        
        #endregion

        #region Fields
        
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        
        #endregion

        #region Constructor
        
        public Accountant(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "$");
        }
        
        #endregion

        #region Public Methods

        /// <summary>
        /// Генерирует HTML отчет с балансами из БД и открывает в браузере
        /// </summary>
        /// <param name="chains">Колонки через запятую, если null - все из _native</param>
        public void ShowBalanceTable(string chains = null, bool single = false, bool call = false)
        {
            var columns = string.IsNullOrEmpty(chains) 
                ? _project.ClmnList("_native") 
                : chains.Split(',').ToList();

            _project.SendInfoToLog("Requesting data from database...", false);
            
            string result = _project.SqlGet(
                $"{string.Join(",", columns)}", 
                "_native", 
                where: $"id <= '{_project.Var("rangeEnd")}' ORDER BY id"
            );

            if (string.IsNullOrEmpty(result))
            {
                _project.SendWarningToLog("No data found in balance table");
                return;
            }

            var rows = result.Trim().Split(_r);
            _project.SendInfoToLog($"Loaded {rows.Length} rows", false);

            string html;
            
            if (columns.Count <= 3 && rows.Length >= 100 && !single)
            {
                int rowsPerBlock = 50;  // Фиксированно 50 строк на блок
                int blocksPerPage = CalculateOptimalBlocksPerPage(columns.Count, rows.Length);
                
                _project.SendInfoToLog($"Using multi-column view: {blocksPerPage} blocks per page", false);
                html = GenerateBalanceHtmlMultiColumn(rows, columns, rowsPerBlock, blocksPerPage);
            }
            else
            {
                _project.SendInfoToLog("Using single-column view", false);
                html = GenerateBalanceHtml(rows, columns);
            }
            
            string tempPath = System.IO.Path.Combine(_project.Path, ".data", "balanceReport.html");
            System.IO.File.WriteAllText(tempPath, html, System.Text.Encoding.UTF8);
            if (call) System.Diagnostics.Process.Start(tempPath);
        }

        /// <summary>
        /// Рассчитывает оптимальное количество блоков на странице
        /// </summary>
        private int CalculateOptimalBlocksPerPage(int columnCount, int totalRows)
        {
            // Примерная ширина одной таблицы в пикселях
            // account (200px) + balance columns (150px каждая) + sum (150px)
            int tableWidth = 200 + ((columnCount - 1) * 150) + 150;
            
            // Примерная ширина экрана (минус отступы)
            int screenWidth = 1900; // Типичный Full HD минус отступы
            
            // Сколько таблиц влезет по ширине
            int tablesPerRow = Math.Max(1, screenWidth / (tableWidth + 15)); // 15px - gap
            
            // Сколько рядов можно показать (чтобы не было слишком длинной страницы)
            int maxRows = 2; // Максимум 2 ряда таблиц на одной странице
            
            int optimalBlocks = tablesPerRow * maxRows;
            
            // Не меньше 2 и не больше 8
            return Math.Max(2, Math.Min(8, optimalBlocks));
        }

        /// <summary>
        /// Генерирует HTML отчет из списка строк формата "account:balance"
        /// </summary>
        public void ShowBalanceTableFromList(List<string> data, bool call = false)
        {
            string html = GenerateBalanceHtmlFromList(data);
            
            string tempPath = System.IO.Path.Combine(_project.Path, ".data", "balanceListReport.html");
            System.IO.File.WriteAllText(tempPath, html, System.Text.Encoding.UTF8);
            if (call) System.Diagnostics.Process.Start(tempPath);
        }

        #endregion

        #region Private Methods - HTML Generation

        private string GenerateBalanceHtml(string[] rows, List<string> columns)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='ru'>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='UTF-8'>");
            sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("    <title>Balance Report</title>");
            
            // === STYLES (Applied from statistics report) ===
            sb.AppendLine("    <style>");
            sb.AppendLine(@"
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body { 
                    font-family: 'Iosevka', 'Consolas', monospace;
                    background: #0d1117;
                    padding: 15px;
                    color: #c9d1d9;
                }
                .container { 
                    max-width: 100%; 
                    margin: 0 auto; 
                    background: #161b22; 
                    border-radius: 6px; 
                    border: 1px solid #30363d; 
                    overflow: hidden; 
                }
                .header {
                    background: #161b22;
                    border-bottom: 1px solid #30363d;
                    color: #c9d1d9;
                    padding: 12px 20px;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                }
                .header h1 { font-size: 18px; font-weight: 600; }
                .header p { color: #8b949e; font-size: 12px; }
                .table-wrapper { overflow-x: auto; padding: 5px; }
                table { width: auto; border-collapse: collapse; font-size: 11px; }
                th:first-child, td:first-child { width: 200px; min-width: 200px; max-width: 200px; }
                th:not(:first-child), td:not(:first-child) { width: 150px; min-width: 50px; max-width: 200px; }
                th { 
                    background: #0d1117; 
                    color: #c9d1d9; 
                    font-weight: 600; 
                    text-align: left; 
                    padding: 8px 10px; 
                    border-bottom: 2px solid #30363d; 
                    border-right: 1px solid #30363d; 
                    position: sticky; top: 0; 
                    z-index: 10; 
                }
                td { padding: 6px 10px; border-bottom: 1px solid #30363d; border-right: 1px solid #30363d; }
                tr:hover { background-color: #21262d; }
                .acc-column { font-family: 'Iosevka', monospace; font-weight: bold; background: #000; color: #fff; padding: 5px !important; }
                .balance-cell { font-family: 'Iosevka', monospace; font-weight: bold; text-align: right; font-size: 10px; }
                .balance-highest { background-color: #4682B4; color: white; }
                .balance-high { background-color: #228B22; color: white; }
                .balance-medium { background-color: #9ACD32; color: #000; }
                .balance-low { background-color: #F0E68C; color: #000; }
                .balance-verylow { background-color: #FFA07A; color: #000; }
                .balance-minimal { background-color: #CD5C5C; color: white; }
                .balance-zero { background-color: transparent; color: #444; }
                .summary-row { font-weight: bold; background: #0d1117 !important; border-top: 2px solid #58a6ff !important; }
                .summary-row td { padding: 8px 10px !important; font-size: 12px; color: #58a6ff; }
                .stats { display: flex; justify-content: space-around; padding: 15px; background: #0d1117; border-top: 1px solid #30363d; }
                .stat-item { text-align: center; }
                .stat-value { font-size: 20px; font-weight: bold; color: #c9d1d9; }
                .stat-label { font-size: 11px; color: #8b949e; margin-top: 4px; }
                .pagination { display: flex; justify-content: center; align-items: center; gap: 8px; padding: 12px; background: #0d1117; border-top: 1px solid #30363d; }
                .pagination button { padding: 6px 14px; background: #21262d; color: #c9d1d9; border: 1px solid #30363d; border-radius: 6px; cursor: pointer; font-size: 12px; font-weight: 500; transition: all 0.2s; }
                .pagination button:hover:not(:disabled) { background: #30363d; border-color: #58a6ff; }
                .pagination button:disabled { background: #21262d; cursor: not-allowed; opacity: 0.4; }
                .pagination .page-info { font-size: 12px; color: #8b949e; min-width: 100px; text-align: center; }
                .hidden { display: none; }
            ");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            
            sb.AppendLine("<div class='container'>");
            
            sb.AppendLine("    <div class='header'>");
            sb.AppendLine("        <h1>💰 Balance Report</h1>");
            sb.AppendLine($"        <p>{DateTime.Now:dd.MM.yyyy HH:mm} | Accounts: {rows.Length}</p>");
            sb.AppendLine("    </div>");
            
            sb.AppendLine("    <div class='table-wrapper'>");
            sb.AppendLine("        <table id='balanceTable'>");
            
            sb.AppendLine("            <thead><tr>");
            foreach (var col in columns)
            {
                sb.AppendLine($"                <th>{col}</th>");
            }
            sb.AppendLine("                <th>Sum</th>");
            sb.AppendLine("            </tr></thead>");
            
            sb.AppendLine("            <tbody>");
            
            var columnSums = new decimal[columns.Count];
            decimal grandTotal = 0;
            
            foreach (var row in rows)
            {
                var values = row.Split(_c);
                if (values.Length != columns.Count) continue;

                sb.AppendLine("            <tr class='data-row'>");
                
                decimal rowSum = 0;
                
                for (int i = 0; i < values.Length; i++)
                {
                    if (i == 0)
                    {
                        sb.AppendLine($"                <td class='acc-column'>{values[i]}</td>");
                    }
                    else
                    {
                        string val = values[i].Replace(",", ".");
                        decimal balance = 0;
                        
                        if (decimal.TryParse(val, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out balance))
                        {
                            columnSums[i] += balance;
                            rowSum += balance;
                        }
                        
                        string cssClass = GetBalanceCssClass(balance);
                        string formatted = FormatBalance(balance);
                        
                        sb.AppendLine($"                <td class='balance-cell {cssClass}'>{formatted}</td>");
                    }
                }
                
                grandTotal += rowSum;
                string rowSumClass = GetBalanceCssClass(rowSum);
                sb.AppendLine($"                <td class='balance-cell {rowSumClass}'>{FormatBalance(rowSum)}</td>");
                sb.AppendLine("            </tr>");
            }
            
            sb.AppendLine("            <tr class='summary-row'>");
            sb.AppendLine("                <td>ИТОГО</td>");
            for (int i = 1; i < columns.Count; i++)
            {
                sb.AppendLine($"                <td class='balance-cell'>{FormatBalance(columnSums[i])}</td>");
            }
            sb.AppendLine($"                <td class='balance-cell'>{FormatBalance(grandTotal)}</td>");
            sb.AppendLine("            </tr>");
            
            sb.AppendLine("            </tbody>");
            sb.AppendLine("        </table>");
            sb.AppendLine("    </div>");
            
            sb.AppendLine("    <div class='pagination'>");
            sb.AppendLine("        <button id='firstBtn' onclick='goToPage(1)'>⏮</button>");
            sb.AppendLine("        <button id='prevBtn' onclick='goToPage(currentPage - 1)'>←</button>");
            sb.AppendLine("        <span class='page-info' id='pageInfo'>Page 1</span>");
            sb.AppendLine("        <button id='nextBtn' onclick='goToPage(currentPage + 1)'>→</button>");
            sb.AppendLine("        <button id='lastBtn' onclick='goToPage(totalPages)'>⏭</button>");
            sb.AppendLine("    </div>");
            
            int accountsWithBalance = 0;
            int accountsAbove01 = 0;
            
            foreach (var row in rows)
            {
                var values = row.Split(_c);
                decimal rowSum = 0;
                
                for (int i = 1; i < values.Length; i++)
                {
                    if (decimal.TryParse(values[i].Replace(",", "."), 
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal b))
                    {
                        rowSum += b;
                    }
                }
                
                if (rowSum > 0) accountsWithBalance++;
                if (rowSum >= 0.1m) accountsAbove01++;
            }
                
            sb.AppendLine("    <div class='stats'>");
            sb.AppendLine("        <div class='stat-item'><div class='stat-value'>" + rows.Length + "</div><div class='stat-label'>Total</div></div>");
            sb.AppendLine("        <div class='stat-item'><div class='stat-value'>" + accountsWithBalance + "</div><div class='stat-label'>Active</div></div>");
            sb.AppendLine("        <div class='stat-item'><div class='stat-value'>" + accountsAbove01 + "</div><div class='stat-label'>≥ 0.1</div></div>");
            sb.AppendLine("        <div class='stat-item'><div class='stat-value'>" + FormatBalance(grandTotal) + "</div><div class='stat-label'>Total</div></div>");
            sb.AppendLine("    </div>");
                
            sb.AppendLine("</div>");
                
            sb.AppendLine("<script>");
            sb.AppendLine($"    const rowsPerPage = {ROWS_PER_PAGE};");
            sb.AppendLine("    let currentPage = 1;");
            sb.AppendLine("    const dataRows = document.querySelectorAll('.data-row');");
            sb.AppendLine("    const totalPages = Math.ceil(dataRows.length / rowsPerPage);");
            sb.AppendLine("    function showPage(page) {");
            sb.AppendLine("        const start = (page - 1) * rowsPerPage;");
            sb.AppendLine("        const end = start + rowsPerPage;");
            sb.AppendLine("        dataRows.forEach((row, index) => { row.classList.toggle('hidden', index < start || index >= end); });");
            sb.AppendLine("    }");
            sb.AppendLine("    function goToPage(page) {");
            sb.AppendLine("        if (page < 1 || page > totalPages) return;");
            sb.AppendLine("        currentPage = page;");
            sb.AppendLine("        showPage(page);");
            sb.AppendLine("        updatePagination();");
            sb.AppendLine("    }");
            sb.AppendLine("    function updatePagination() {");
            sb.AppendLine("        document.getElementById('pageInfo').textContent = `${currentPage}/${totalPages}`;");
            sb.AppendLine("        document.getElementById('firstBtn').disabled = currentPage === 1;");
            sb.AppendLine("        document.getElementById('prevBtn').disabled = currentPage === 1;");
            sb.AppendLine("        document.getElementById('nextBtn').disabled = currentPage === totalPages;");
            sb.AppendLine("        document.getElementById('lastBtn').disabled = currentPage === totalPages;");
            sb.AppendLine("    }");
            sb.AppendLine("    showPage(1);");
            sb.AppendLine("    updatePagination();");
            sb.AppendLine("    document.addEventListener('keydown', function(e) {");
            sb.AppendLine("        if (e.key === 'ArrowLeft') goToPage(currentPage - 1);");
            sb.AppendLine("        if (e.key === 'ArrowRight') goToPage(currentPage + 1);");
            sb.AppendLine("    });");
            sb.AppendLine("</script>");
                
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
                
            return sb.ToString();
        }

        /// <summary>
        /// Generates a single-column HTML report for balances with styles from the statistics report.
        /// </summary>
        private string GenerateBalanceHtmlFromList(List<string> data)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='ru'>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='UTF-8'>");
            sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("    <title>Balance Report</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine(@"
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body { 
                    font-family: 'Iosevka', 'Consolas', monospace;
                    background: #0d1117;
                    padding: 15px;
                    color: #c9d1d9;
                }
                .container { 
                    width: fit-content; 
                    max-width: 90%; 
                    margin: 0 auto; 
                    background: #161b22; 
                    border-radius: 6px; 
                    border: 1px solid #30363d; 
                    overflow: hidden; 
                }
                .header {
                    background: #161b22;
                    border-bottom: 1px solid #30363d;
                    color: #c9d1d9;
                    padding: 20px;
                    text-align: center;
                }
                .header h1 { font-size: 24px; font-weight: 600; margin-bottom: 8px; }
                .header p { color: #8b949e; font-size: 12px; }
                .table-wrapper { overflow-x: auto; padding: 10px; }
                table { width: auto; border-collapse: collapse; font-size: 11px; }
                th { 
                    background: #0d1117; 
                    color: #c9d1d9; 
                    font-weight: 600; 
                    text-align: left; 
                    padding: 10px 12px; 
                    border-bottom: 2px solid #30363d; 
                    position: sticky; top: 0; 
                    z-index: 10; 
                }
                td { padding: 8px 12px; border-bottom: 1px solid #30363d; }
                tr:hover { background-color: #21262d; }
                .balance-cell { font-family: 'Iosevka', monospace; font-weight: bold; text-align: right; font-size: 11px; }
                .balance-highest { background-color: #4682B4; color: white; }
                .balance-high { background-color: #228B22; color: white; }
                .balance-medium { background-color: #9ACD32; color: #000; }
                .balance-low { background-color: #F0E68C; color: #000; }
                .balance-verylow { background-color: #FFA07A; color: #000; }
                .balance-minimal { background-color: #CD5C5C; color: white; }
                .balance-zero { background-color: transparent; color: #444; }
                .summary-row { font-weight: bold; background: #0d1117 !important; border-top: 2px solid #58a6ff !important; }
                .summary-row td { padding: 10px 12px !important; font-size: 13px; color: #58a6ff; }
                .pagination { display: flex; justify-content: center; align-items: center; gap: 8px; padding: 15px; background: #0d1117; border-top: 1px solid #30363d; }
                .pagination button { padding: 8px 16px; background: #21262d; color: #c9d1d9; border: 1px solid #30363d; border-radius: 6px; cursor: pointer; font-size: 13px; font-weight: 500; transition: all 0.2s; }
                .pagination button:hover:not(:disabled) { background: #30363d; border-color: #58a6ff; }
                .pagination button:disabled { background: #21262d; cursor: not-allowed; opacity: 0.4; }
                .pagination .page-info { font-size: 13px; color: #8b949e; min-width: 120px; text-align: center; }
                .hidden { display: none; }
            ");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            
            sb.AppendLine("<div class='container'>");
            sb.AppendLine("    <div class='header'>");
            sb.AppendLine("        <h1>💰 Balance Report</h1>");
            sb.AppendLine($"        <p>Generated: {DateTime.Now:dd.MM.yyyy HH:mm:ss} | Total accounts: {data.Count}</p>");
            sb.AppendLine("    </div>");
            
            sb.AppendLine("    <div class='table-wrapper'>");
            sb.AppendLine("        <table id='balanceTable'>");
            sb.AppendLine("            <thead><tr><th>Счет</th><th>Баланс</th></tr></thead>");
            sb.AppendLine("            <tbody>");
            
            decimal totalSum = 0;
            
            foreach (var line in data)
            {
                var parts = line.Split(':');
                if (parts.Length != 2) continue;

                string account = parts[0].Trim();
                string balanceStr = parts[1].Replace(",", ".").Trim();
                
                if (!decimal.TryParse(balanceStr, System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out decimal balance))
                {
                    balance = 0;
                }

                totalSum += balance;
                string cssClass = GetBalanceCssClass(balance);
                
                sb.AppendLine("            <tr class='data-row'>");
                sb.AppendLine($"                <td>{account}</td>");
                sb.AppendLine($"                <td class='balance-cell {cssClass}'>{FormatBalance(balance)}</td>");
                sb.AppendLine("            </tr>");
            }
            
            sb.AppendLine("            <tr class='summary-row'>");
            sb.AppendLine("                <td>TOTAL</td>");
            sb.AppendLine($"                <td class='balance-cell'>{FormatBalance(totalSum)}</td>");
            sb.AppendLine("            </tr>");
            
            sb.AppendLine("            </tbody>");
            sb.AppendLine("        </table>");
            sb.AppendLine("    </div>");
            
            sb.AppendLine("    <div class='pagination'>");
            sb.AppendLine("        <button id='firstBtn' onclick='goToPage(1)'>⏮ First</button>");
            sb.AppendLine("        <button id='prevBtn' onclick='goToPage(currentPage - 1)'>← Prev</button>");
            sb.AppendLine("        <span class='page-info' id='pageInfo'>Page 1</span>");
            sb.AppendLine("        <button id='nextBtn' onclick='goToPage(currentPage + 1)'>Next →</button>");
            sb.AppendLine("        <button id='lastBtn' onclick='goToPage(totalPages)'>Last ⏭</button>");
            sb.AppendLine("    </div>");
            
            sb.AppendLine("</div>");
            
            sb.AppendLine("<script>");
            sb.AppendLine($"    const rowsPerPage = {ROWS_PER_PAGE};");
            sb.AppendLine("    let currentPage = 1;");
            sb.AppendLine("    const dataRows = document.querySelectorAll('.data-row');");
            sb.AppendLine("    const totalPages = Math.ceil(dataRows.length / rowsPerPage);");
            sb.AppendLine("    function showPage(page) {");
            sb.AppendLine("        const start = (page - 1) * rowsPerPage;");
            sb.AppendLine("        const end = start + rowsPerPage;");
            sb.AppendLine("        dataRows.forEach((row, index) => { row.classList.toggle('hidden', index < start || index >= end); });");
            sb.AppendLine("    }");
            sb.AppendLine("    function goToPage(page) {");
            sb.AppendLine("        if (page < 1 || page > totalPages) return;");
            sb.AppendLine("        currentPage = page;");
            sb.AppendLine("        showPage(page);");
            sb.AppendLine("        updatePagination();");
            sb.AppendLine("    }");
            sb.AppendLine("    function updatePagination() {");
            sb.AppendLine("        document.getElementById('pageInfo').textContent = `Page ${currentPage} of ${totalPages}`;");
            sb.AppendLine("        document.getElementById('firstBtn').disabled = currentPage === 1;");
            sb.AppendLine("        document.getElementById('prevBtn').disabled = currentPage === 1;");
            sb.AppendLine("        document.getElementById('nextBtn').disabled = currentPage === totalPages;");
            sb.AppendLine("        document.getElementById('lastBtn').disabled = currentPage === totalPages;");
            sb.AppendLine("    }");
            sb.AppendLine("    showPage(1);");
            sb.AppendLine("    updatePagination();");
            sb.AppendLine("    document.addEventListener('keydown', function(e) {");
            sb.AppendLine("        if (e.key === 'ArrowLeft') goToPage(currentPage - 1);");
            sb.AppendLine("        if (e.key === 'ArrowRight') goToPage(currentPage + 1);");
            sb.AppendLine("    });");
            sb.AppendLine("</script>");
            
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
                
            return sb.ToString();
        }

        /// <summary>
        /// Generates a multi-column HTML report with styles from the statistics report.
        /// </summary>
        private string GenerateBalanceHtmlMultiColumn(string[] rows, List<string> columns, int rowsPerBlock = 50, int blocksPerPage = 4)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='ru'>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='UTF-8'>");
            sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("    <title>Balance Report - Multi Column</title>");
            
            sb.AppendLine("    <style>");
            sb.AppendLine(@"
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body { 
                    font-family: 'Iosevka', 'Consolas', monospace;
                    background: #0d1117;
                    padding: 15px;
                    color: #c9d1d9;
                }
                .container { 
                    max-width: 100%; 
                    margin: 0 auto; 
                    background: #161b22; 
                    border-radius: 6px; 
                    border: 1px solid #30363d; 
                    overflow: hidden; 
                }
                .header {
                    background: #161b22;
                    border-bottom: 1px solid #30363d;
                    color: #c9d1d9;
                    padding: 12px 20px;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                }
                .header h1 { font-size: 18px; font-weight: 600; }
                .header p { color: #8b949e; font-size: 12px; }
                .tables-wrapper { padding: 15px; overflow-x: auto; }
                .tables-container { display: flex; gap: 15px; flex-wrap: nowrap; }
                .table-block { flex: 0 0 auto; border: 1px solid #30363d; border-radius: 6px; overflow: hidden; background: #0d1117; }
                .block-header { font-size: 11px; font-weight: 600; padding: 6px 10px; background: #21262d; color: #8b949e; text-align: center; border-bottom: 1px solid #30363d; }
                table { width: auto; border-collapse: collapse; font-size: 11px; }
                th { background: #0d1117; color: #c9d1d9; font-weight: 600; text-align: left; padding: 8px 10px; border-bottom: 2px solid #30363d; border-right: 1px solid #30363d; }
                td { padding: 6px 10px; border-bottom: 1px solid #30363d; border-right: 1px solid #30363d; }
                tr:hover { background-color: #21262d; }
                th:first-child, td:first-child { width: 50px; min-width: 50px; }
                th:not(:first-child), td:not(:first-child) { width: 100px; min-width: 50px; }
                .acc-column { font-family: 'Iosevka', monospace; font-weight: bold; background: #000; color: #fff; padding: 5px !important; }
                .balance-cell { font-family: 'Iosevka', monospace; font-weight: bold; text-align: right; font-size: 10px; }
                .balance-highest { background-color: #4682B4; color: white; }
                .balance-high { background-color: #228B22; color: white; }
                .balance-medium { background-color: #9ACD32; color: #000; }
                .balance-low { background-color: #F0E68C; color: #000; }
                .balance-verylow { background-color: #FFA07A; color: #000; }
                .balance-minimal { background-color: #CD5C5C; color: white; }
                .balance-zero { background-color: transparent; color: #444; }
                .summary-row { font-weight: bold; background: #21262d !important; border-top: 2px solid #58a6ff !important; }
                .summary-row td { padding: 8px 10px !important; font-size: 11px; color: #58a6ff; }
                .stats { display: flex; justify-content: space-around; padding: 15px; background: #0d1117; border-top: 1px solid #30363d; }
                .stat-item { text-align: center; }
                .stat-value { font-size: 20px; font-weight: bold; color: #c9d1d9; }
                .stat-label { font-size: 11px; color: #8b949e; margin-top: 4px; }
                .pagination { display: flex; justify-content: center; align-items: center; gap: 8px; padding: 12px; background: #0d1117; border-top: 1px solid #30363d; }
                .pagination button { padding: 6px 14px; background: #21262d; color: #c9d1d9; border: 1px solid #30363d; border-radius: 6px; cursor: pointer; font-size: 12px; font-weight: 500; transition: all 0.2s; }
                .pagination button:hover:not(:disabled) { background: #30363d; border-color: #58a6ff; }
                .pagination button:disabled { background: #21262d; cursor: not-allowed; opacity: 0.4; }
                .pagination .page-info { font-size: 12px; color: #8b949e; min-width: 100px; text-align: center; }
                .hidden { display: none; }
            ");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            
            sb.AppendLine("<div class='container'>");
            sb.AppendLine("    <div class='header'>");
            sb.AppendLine("        <h1>Balance Report - Multi Column View</h1>");
            sb.AppendLine($"        <p>{DateTime.Now:dd.MM.yyyy HH:mm} | Accounts: {rows.Length} | Blocks: {(int)Math.Ceiling((double)rows.Length / rowsPerBlock)}</p>");
            sb.AppendLine("    </div>");
            
            var blocks = new List<List<string>>();
            for (int i = 0; i < rows.Length; i += rowsPerBlock)
            {
                blocks.Add(rows.Skip(i).Take(rowsPerBlock).ToList());
            }
            
            sb.AppendLine("    <div class='tables-wrapper'>");
            sb.AppendLine("        <div class='tables-container'>");
            
            for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
            {
                var blockRows = blocks[blockIndex];
                int startRow = blockIndex * rowsPerBlock + 1;
                int endRow = startRow + blockRows.Count - 1;
                
                sb.AppendLine($"        <div class='table-block' data-block='{blockIndex}'>");
                sb.AppendLine($"            <div class='block-header'>Rows {startRow}-{endRow}</div>");
                sb.AppendLine("            <table>");
                
                sb.AppendLine("                <thead><tr>");
                foreach (var col in columns)
                {
                    sb.AppendLine($"                    <th>{col}</th>");
                }
                sb.AppendLine("                    <th>Sum</th>");
                sb.AppendLine("                </tr></thead>");
                
                sb.AppendLine("                <tbody>");
                
                var columnSums = new decimal[columns.Count];
                decimal blockTotal = 0;
                
                foreach (var row in blockRows)
                {
                    var values = row.Split(_c);
                    if (values.Length != columns.Count) continue;

                    sb.AppendLine("                <tr>");
                    decimal rowSum = 0;
                    
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (i == 0)
                        {
                            sb.AppendLine($"                    <td class='acc-column'>{values[i]}</td>");
                        }
                        else
                        {
                            string val = values[i].Replace(",", ".");
                            if (decimal.TryParse(val, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out decimal balance))
                            {
                                columnSums[i] += balance;
                                rowSum += balance;
                            }
                            
                            string cssClass = GetBalanceCssClass(rowSum);
                            string formatted = FormatBalance(rowSum);
                            sb.AppendLine($"                    <td class='balance-cell {cssClass}'>{formatted}</td>");
                        }
                    }
                    
                    blockTotal += rowSum;
                    string rowSumClass = GetBalanceCssClass(rowSum);
                    sb.AppendLine($"                    <td class='balance-cell {rowSumClass}'>{FormatBalance(rowSum)}</td>");
                    sb.AppendLine("                </tr>");
                }
                
                sb.AppendLine("                <tr class='summary-row'>");
                sb.AppendLine("                    <td>BLOCK TOTAL</td>");
                for (int i = 1; i < columns.Count; i++)
                {
                    sb.AppendLine($"                    <td class='balance-cell'>{FormatBalance(columnSums[i])}</td>");
                }
                sb.AppendLine($"                    <td class='balance-cell'>{FormatBalance(blockTotal)}</td>");
                sb.AppendLine("                </tr>");
                
                sb.AppendLine("                </tbody>");
                sb.AppendLine("            </table>");
                sb.AppendLine("        </div>");
            }
            
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            
            sb.AppendLine("    <div class='pagination'>");
            sb.AppendLine("        <button id='firstBtn' onclick='goToPage(1)'>⏮</button>");
            sb.AppendLine("        <button id='prevBtn' onclick='goToPage(currentPage - 1)'>←</button>");
            sb.AppendLine("        <span class='page-info' id='pageInfo'>Page 1</span>");
            sb.AppendLine("        <button id='nextBtn' onclick='goToPage(currentPage + 1)'>→</button>");
            sb.AppendLine("        <button id='lastBtn' onclick='goToPage(totalPages)'>⏭</button>");
            sb.AppendLine("    </div>");
            
            int accountsWithBalance = 0;
            int accountsAbove01 = 0;
            decimal grandTotal = 0;
            
            foreach (var row in rows)
            {
                var values = row.Split(_c);
                decimal rowSum = 0;
                for (int i = 1; i < values.Length; i++)
                {
                    if (decimal.TryParse(values[i].Replace(",", "."), 
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out decimal b))
                    {
                        rowSum += b;
                    }
                }
                grandTotal += rowSum;
                if (rowSum > 0) accountsWithBalance++;
                if (rowSum >= 0.1m) accountsAbove01++;
            }
            
            sb.AppendLine("    <div class='stats'>");
            sb.AppendLine("        <div class='stat-item'><div class='stat-value'>" + rows.Length + "</div><div class='stat-label'>Total</div></div>");
            sb.AppendLine("        <div class='stat-item'><div class='stat-value'>" + accountsWithBalance + "</div><div class='stat-label'>Active</div></div>");
            sb.AppendLine("        <div class='stat-item'><div class='stat-value'>" + accountsAbove01 + "</div><div class='stat-label'>≥ 0.1</div></div>");
            sb.AppendLine("        <div class='stat-item'><div class='stat-value'>" + FormatBalance(grandTotal) + "</div><div class='stat-label'>Grand Total</div></div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</div>");
            
            sb.AppendLine("<script>");
            sb.AppendLine($"    const blocksPerPage = {blocksPerPage};");
            sb.AppendLine("    let currentPage = 1;");
            sb.AppendLine("    const tableBlocks = document.querySelectorAll('.table-block');");
            sb.AppendLine("    const totalPages = Math.ceil(tableBlocks.length / blocksPerPage);");
            sb.AppendLine("    function showPage(page) {");
            sb.AppendLine("        const start = (page - 1) * blocksPerPage;");
            sb.AppendLine("        const end = start + blocksPerPage;");
            sb.AppendLine("        tableBlocks.forEach((block, index) => { block.classList.toggle('hidden', index < start || index >= end); });");
            sb.AppendLine("    }");
            sb.AppendLine("    function goToPage(page) {");
            sb.AppendLine("        if (page < 1 || page > totalPages) return;");
            sb.AppendLine("        currentPage = page;");
            sb.AppendLine("        showPage(page);");
            sb.AppendLine("        updatePagination();");
            sb.AppendLine("    }");
            sb.AppendLine("    function updatePagination() {");
            sb.AppendLine("        document.getElementById('pageInfo').textContent = `${currentPage}/${totalPages}`;");
            sb.AppendLine("        document.getElementById('firstBtn').disabled = currentPage === 1;");
            sb.AppendLine("        document.getElementById('prevBtn').disabled = currentPage === 1;");
            sb.AppendLine("        document.getElementById('nextBtn').disabled = currentPage === totalPages;");
            sb.AppendLine("        document.getElementById('lastBtn').disabled = currentPage === totalPages;");
            sb.AppendLine("    }");
            sb.AppendLine("    showPage(1);");
            sb.AppendLine("    updatePagination();");
            sb.AppendLine("    document.addEventListener('keydown', function(e) {");
            sb.AppendLine("        if (e.key === 'ArrowLeft') goToPage(currentPage - 1);");
            sb.AppendLine("        if (e.key === 'ArrowRight') goToPage(currentPage + 1);");
            sb.AppendLine("    });");
            sb.AppendLine("</script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            return sb.ToString();
        }   
        
        #endregion

        #region Private Methods - Helpers

        private string FormatBalance(decimal balance)
        {
            return balance.ToString(BALANCE_FORMAT, System.Globalization.CultureInfo.InvariantCulture);
        }

        private string GetBalanceCssClass(decimal balance)
        {
            if (balance >= BALANCE_THRESHOLD_HIGHEST) return "balance-highest";
            if (balance >= BALANCE_THRESHOLD_HIGH) return "balance-high";
            if (balance >= BALANCE_THRESHOLD_MEDIUM) return "balance-medium";
            if (balance >= BALANCE_THRESHOLD_LOW) return "balance-low";
            if (balance >= BALANCE_THRESHOLD_VERYLOW) return "balance-verylow";
            if (balance > 0) return "balance-minimal";
            if (balance == 0) return "balance-zero";
            return "";
        }

        #endregion
    }
    
  }
  namespace z3nCore
  {
      public static partial class ProjectExtensions
      {
          public static void GenerateNative(this IZennoPosterProjectModel project, string chains, bool call = false)
          {
              new Utilities.Accountant(project).ShowBalanceTable("id," + chains, call:call);
          }

          public static void GenerateToken(this IZennoPosterProjectModel project, string contract, string chain, string mode = "Erc20", bool call = false)
          {
              var res = new List<string>();
              if (mode == "Erc20")
                  res = Erc20(project, contract, chain);
              else
                  res = Erc721(project, contract, chain);

              new Utilities.Accountant(project).ShowBalanceTableFromList(res, call:call);

          }

          private static List<string> Erc20(IZennoPosterProjectModel project, string contract, string chain)
          {
              var res = new List<string>();
              var range = project.Range();
              foreach (string acc in range)
              {
                  project.Var("acc0", acc);
                  string address = project.DbGet("evm_pk", "_addresses");

                  try
                  {
                      var balance = W3bTools.ERC20(contract, Rpc.Get(chain), address);
                      res.Add($"{acc}:{balance}");
                  }
                  catch (Exception ex)
                  {
                      project.warn(ex.Message);
                  }
              }

              return res;
          }

          private static List<string> Erc721(IZennoPosterProjectModel project, string contract, string chain)
          {
              var res = new List<string>();
              var range = project.Range();
              foreach (string acc in range)
              {
                  project.Var("acc0", acc);
                  string address = project.DbGet("evm_pk", "_addresses");

                  try
                  {
                      var balance = W3bTools.ERC721(contract, Rpc.Get(chain), address);
                      res.Add($"{acc}:{balance}");
                  }
                  catch (Exception ex)
                  {
                      project.warn(ex.Message);
                  }
              }

              return res;
          }

      }
  }