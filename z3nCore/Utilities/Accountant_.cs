using System;
using System.Collections.Generic;
using System.Linq;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.Utilities
{
    public class _Accountant
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
        
        #endregion

        #region Fields
        
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        
        #endregion

        #region Constructor
        
        public _Accountant(IZennoPosterProjectModel project, bool log = false)
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
        public void ShowBalanceTable(string chains = null, bool single = false)
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

            var rows = result.Trim().Split('\n');
            _project.SendInfoToLog($"Loaded {rows.Length} rows", false);

            // Автоматический выбор метода
            string html;
            
            // Если колонок <= 3 и строк >= 100 - используем мульти-колонки
            if (columns.Count <= 3 && rows.Length >= 100 && !single)
            {
                // Рассчитываем оптимальные параметры
                int rowsPerBlock = 50;  // Фиксированно 50 строк на блок
                int blocksPerPage = CalculateOptimalBlocksPerPage(columns.Count, rows.Length);
                
                _project.SendInfoToLog($"Using multi-column view: {blocksPerPage} blocks per page", false);
                html = GenerateBalanceHtmlMultiColumn(rows, columns, rowsPerBlock, blocksPerPage);
            }
            else
            {
                // Обычный режим для многоколоночных данных или по запросу
                _project.SendInfoToLog("Using single-column view", false);
                html = GenerateBalanceHtml(rows, columns);
            }
            
            string tempPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), 
                $"balance_report_{DateTime.Now:yyyyMMdd_HHmmss}.html"
            );
            
            System.IO.File.WriteAllText(tempPath, html, System.Text.Encoding.UTF8);
            _project.SendInfoToLog($"Report saved to: {tempPath}", false);
            
            System.Diagnostics.Process.Start(tempPath);
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
        public void ShowBalanceTableFromList(List<string> data)
        {
            string html = GenerateBalanceHtmlFromList(data);
            
            string tempPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), 
                $"balance_report_{DateTime.Now:yyyyMMdd_HHmmss}.html"
            );
            
            System.IO.File.WriteAllText(tempPath, html, System.Text.Encoding.UTF8);
            _project.SendInfoToLog($"Report saved to: {tempPath}", false);
            
            System.Diagnostics.Process.Start(tempPath);
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
        
        // === СТИЛИ ===
        sb.AppendLine("    <style>");
        
        sb.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
        
        // Минимальные отступы, максимум места для данных
        sb.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f5f5; padding: 5px; }");
        
        sb.AppendLine("        .container { max-width: 100%; margin: 0 auto; background: white; border-radius: 4px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); overflow: hidden; }");
        
        // Компактная шапка - одна строка, минимальная высота
        sb.AppendLine("        .header { background: #e9ecef; color: #333; padding: 8px 15px; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #dee2e6; }");
        sb.AppendLine("        .header h1 { font-size: 16px; font-weight: 600; }");
        sb.AppendLine("        .header p { font-size: 11px; color: #6c757d; }");
        
        sb.AppendLine("        .table-wrapper { overflow-x: auto; padding: 5px; }");
        
        // Уменьшенный шрифт таблицы
        sb.AppendLine("        table { width: auto; border-collapse: collapse; font-size: 11px; }");
        sb.AppendLine("        th:first-child, td:first-child { width: 200px; min-width: 200px; max-width: 200px; }");
        sb.AppendLine("        th:not(:first-child), td:not(:first-child) { width: 150px; min-width: 50px; max-width: 200px; }");
        // Компактные заголовки
        sb.AppendLine("        th { background: #f8f9fa; color: #333; font-weight: 600; text-align: left; padding: 6px 5px; border-bottom: 2px solid #dee2e6; border-right: 1px solid #dee2e6; position: sticky; top: 0; z-index: 10; }");
        
        // Компактные ячейки
        sb.AppendLine("        td { padding: 5px; border-bottom: 1px solid #dee2e6; border-right: 1px solid #dee2e6; }");
        
        sb.AppendLine("        tr:hover { background-color: #f8f9fa; }");
        
        sb.AppendLine("        .acc-column { font-family: 'Iosevka' monospace; font-weight: bold; background: #000; color: #fff; padding: 5px !important; }");
        
        // Уменьшенный шрифт балансов
        sb.AppendLine("        .balance-cell { font-family: 'Iosevka', monospace; font-weight: bold; text-align: right; font-size: 10px; }");
        
        sb.AppendLine("        .balance-highest { background-color: #4682B4; color: white; }");
        sb.AppendLine("        .balance-high { background-color: #228B22; color: white; }");
        sb.AppendLine("        .balance-medium { background-color: #9ACD32; }");
        sb.AppendLine("        .balance-low { background-color: #F0E68C; }");
        sb.AppendLine("        .balance-verylow { background-color: #FFA07A; }");
        sb.AppendLine("        .balance-minimal { background-color: #CD5C5C; color: white; }");
        sb.AppendLine("        .balance-zero { background-color: #fff; color: #fff; }");
        
        sb.AppendLine("        .summary-row { font-weight: bold; background: #e9ecef !important; border-top: 3px solid #333 !important; }");
        sb.AppendLine("        .summary-row td { padding: 7px 5px !important; font-size: 11px; }");
        
        // Компактная статистика
        sb.AppendLine("        .stats { display: flex; justify-content: space-around; padding: 8px; background: #f8f9fa; border-top: 1px solid #dee2e6; }");
        sb.AppendLine("        .stat-item { text-align: center; }");
        sb.AppendLine("        .stat-value { font-size: 16px; font-weight: bold; color: #333; }");
        sb.AppendLine("        .stat-label { font-size: 10px; color: #6c757d; margin-top: 2px; }");
        
        // Компактная пагинация
        sb.AppendLine("        .pagination { display: flex; justify-content: center; align-items: center; gap: 5px; padding: 8px; background: #f8f9fa; border-top: 1px solid #dee2e6; }");
        
        // Маленькие кнопки
        sb.AppendLine("        .pagination button { padding: 5px 12px; background: #495057; color: white; border: none; border-radius: 3px; cursor: pointer; font-size: 11px; font-weight: 500; transition: background 0.2s; }");
        sb.AppendLine("        .pagination button:hover:not(:disabled) { background: #343a40; }");
        sb.AppendLine("        .pagination button:disabled { background: #ccc; cursor: not-allowed; opacity: 0.6; }");
        
        sb.AppendLine("        .pagination .page-info { font-size: 11px; color: #6c757d; min-width: 100px; text-align: center; }");
        
        sb.AppendLine("        .hidden { display: none; }");
        
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        
        sb.AppendLine("<div class='container'>");
        
        // === КОМПАКТНАЯ ШАПКА ===
        sb.AppendLine("    <div class='header'>");
        sb.AppendLine("        <h1>Balance Report</h1>");
        sb.AppendLine($"        <p>{DateTime.Now:dd.MM.yyyy HH:mm} | Accounts: {rows.Length}</p>");
        sb.AppendLine("    </div>");
        
        // === ТАБЛИЦА ===
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
            var values = row.Split('|');
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
        
        // === КОМПАКТНАЯ ПАГИНАЦИЯ ===
        sb.AppendLine("    <div class='pagination'>");
        sb.AppendLine("        <button id='firstBtn' onclick='goToPage(1)'>⏮</button>");
        sb.AppendLine("        <button id='prevBtn' onclick='goToPage(currentPage - 1)'>←</button>");
        sb.AppendLine("        <span class='page-info' id='pageInfo'>Page 1</span>");
        sb.AppendLine("        <button id='nextBtn' onclick='goToPage(currentPage + 1)'>→</button>");
        sb.AppendLine("        <button id='lastBtn' onclick='goToPage(totalPages)'>⏭</button>");
        sb.AppendLine("    </div>");
        
        // === КОМПАКТНАЯ СТАТИСТИКА ===
        int accountsWithBalance = 0;
        int accountsAbove01 = 0;
        
        foreach (var row in rows)
        {
            var values = row.Split('|');
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
            
            sb.AppendLine("        <div class='stat-item'>");
            sb.AppendLine($"            <div class='stat-value'>{rows.Length}</div>");
            sb.AppendLine("            <div class='stat-label'>Total</div>");
            sb.AppendLine("        </div>");
            
            sb.AppendLine("        <div class='stat-item'>");
            sb.AppendLine($"            <div class='stat-value'>{accountsWithBalance}</div>");
            sb.AppendLine("            <div class='stat-label'>Active</div>");
            sb.AppendLine("        </div>");
            
            sb.AppendLine("        <div class='stat-item'>");
            sb.AppendLine($"            <div class='stat-value'>{accountsAbove01}</div>");
            sb.AppendLine("            <div class='stat-label'>≥ 0.1</div>");
            sb.AppendLine("        </div>");
            
            sb.AppendLine("        <div class='stat-item'>");
            sb.AppendLine($"            <div class='stat-value'>{FormatBalance(grandTotal)}</div>");
            sb.AppendLine("            <div class='stat-label'>Total</div>");
            sb.AppendLine("        </div>");
            
            sb.AppendLine("    </div>");
            
            sb.AppendLine("</div>");
            
            // === JAVASCRIPT ===
            sb.AppendLine("<script>");
            
            sb.AppendLine($"    const rowsPerPage = {ROWS_PER_PAGE};");
            sb.AppendLine("    let currentPage = 1;");
            sb.AppendLine("    const dataRows = document.querySelectorAll('.data-row');");
            sb.AppendLine("    const totalPages = Math.ceil(dataRows.length / rowsPerPage);");
            
            sb.AppendLine("    function showPage(page) {");
            sb.AppendLine("        const start = (page - 1) * rowsPerPage;");
            sb.AppendLine("        const end = start + rowsPerPage;");
            sb.AppendLine("        dataRows.forEach((row, index) => {");
            sb.AppendLine("            row.classList.toggle('hidden', index < start || index >= end);");
            sb.AppendLine("        });");
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
            sb.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
            sb.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f5f5; padding: 20px; }");
            sb.AppendLine("        .container { width: fit-content; max-width: 90%; margin: 0 auto; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); overflow: hidden; }");
            sb.AppendLine("        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }");
            sb.AppendLine("        .header h1 { font-size: 32px; margin-bottom: 10px; }");
            sb.AppendLine("        .header p { font-size: 14px; opacity: 0.9; }");
            sb.AppendLine("        .table-wrapper { padding: 20px; overflow-x: auto; }");
            sb.AppendLine("        .scroll-top { overflow-x: auto; overflow-y: hidden; height: 20px; margin-bottom: 10px; }");
            sb.AppendLine("        .scroll-content { height: 1px;}");
            
            sb.AppendLine("        table { width: auto; border-collapse: collapse; font-size: 11px; }");
            sb.AppendLine("        th, td { max-width: 250px; min-width: 80px; }");
            sb.AppendLine("        th:first-child, td.acc-column { width: 180px; max-width: 180px; min-width: 180px; }");
            sb.AppendLine("        .balance-cell { width: 120px; max-width: 150px; }");

            sb.AppendLine("        th { background: #f8f9fa; color: #333; font-weight: 600; text-align: left; padding: 12px; border-bottom: 2px solid #dee2e6; position: sticky; top: 0; z-index: 10; }");
            sb.AppendLine("        td { padding: 10px 12px; border-bottom: 1px solid #dee2e6; }");
            sb.AppendLine("        tr:hover { background-color: #f8f9fa; }");
            sb.AppendLine("        .balance-cell { font-family: 'Lucida Console', monospace; text-align: right; }");
            sb.AppendLine("        .balance-highest { background-color: #4682B4; color: white; }");
            sb.AppendLine("        .balance-high { background-color: #228B22; color: white; }");
            sb.AppendLine("        .balance-medium { background-color: #9ACD32; }");
            sb.AppendLine("        .balance-low { background-color: #F0E68C; }");
            sb.AppendLine("        .balance-verylow { background-color: #FFA07A; }");
            sb.AppendLine("        .balance-minimal { background-color: #CD5C5C; color: white; }");
            sb.AppendLine("        .balance-zero { background-color: #fff; color: #fff; }");
            sb.AppendLine("        .summary-row { font-weight: bold; background: #e9ecef !important; border-top: 3px solid #333 !important; }");
            sb.AppendLine("        .summary-row td { padding: 14px 12px !important; font-size: 14px; }");
            sb.AppendLine("        .pagination { display: flex; justify-content: center; align-items: center; gap: 10px; padding: 20px; background: #f8f9fa; border-top: 1px solid #dee2e6; }");
            sb.AppendLine("        .pagination button { padding: 10px 20px; background: #667eea; color: white; border: none; border-radius: 5px; cursor: pointer; font-size: 14px; font-weight: 600; transition: background 0.3s; }");
            sb.AppendLine("        .pagination button:hover:not(:disabled) { background: #5568d3; }");
            sb.AppendLine("        .pagination button:disabled { background: #ccc; cursor: not-allowed; opacity: 0.6; }");
            sb.AppendLine("        .pagination .page-info { font-size: 14px; color: #6c757d; min-width: 150px; text-align: center; }");
            sb.AppendLine("        .hidden { display: none; }");
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
            sb.AppendLine("            <thead><tr>");
            sb.AppendLine("                <th>Счет</th>");
            sb.AppendLine("                <th>Баланс</th>");
            sb.AppendLine("            </tr></thead>");
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
            
            // Пагинация
            sb.AppendLine("    <div class='pagination'>");
            sb.AppendLine("        <button id='firstBtn' onclick='goToPage(1)'>⏮ First</button>");
            sb.AppendLine("        <button id='prevBtn' onclick='goToPage(currentPage - 1)'>← Prev</button>");
            sb.AppendLine("        <span class='page-info' id='pageInfo'>Page 1</span>");
            sb.AppendLine("        <button id='nextBtn' onclick='goToPage(currentPage + 1)'>Next →</button>");
            sb.AppendLine("        <button id='lastBtn' onclick='goToPage(totalPages)'>Last ⏭</button>");
            sb.AppendLine("    </div>");
            
            sb.AppendLine("</div>");
            
            // JavaScript для пагинации
            sb.AppendLine("<script>");
            sb.AppendLine($"    const rowsPerPage = {ROWS_PER_PAGE};");
            sb.AppendLine("    let currentPage = 1;");
            sb.AppendLine("    const dataRows = document.querySelectorAll('.data-row');");
            sb.AppendLine("    const totalPages = Math.ceil(dataRows.length / rowsPerPage);");
            sb.AppendLine("    function showPage(page) {");
            sb.AppendLine("        const start = (page - 1) * rowsPerPage;");
            sb.AppendLine("        const end = start + rowsPerPage;");
            sb.AppendLine("        dataRows.forEach((row, index) => {");
            sb.AppendLine("            row.classList.toggle('hidden', index < start || index >= end);");
            sb.AppendLine("        });");
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
            sb.AppendLine("    const scrollTop = document.getElementById('scrollTop');");
            sb.AppendLine("    const tableWrapper = document.querySelector('.table-wrapper');");
            sb.AppendLine("    const table = document.getElementById('balanceTable');");
            sb.AppendLine("    scrollTop.querySelector('.scroll-content').style.width = table.offsetWidth + 'px';");
            sb.AppendLine("    scrollTop.addEventListener('scroll', function() { tableWrapper.scrollLeft = scrollTop.scrollLeft;});");
            sb.AppendLine("    tableWrapper.addEventListener('scroll', function() { scrollTop.scrollLeft = tableWrapper.scrollLeft;});");

            sb.AppendLine("</script>");
            
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            return sb.ToString();
        }

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
            sb.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
            sb.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f5f5; padding: 5px; }");
            sb.AppendLine("        .container { max-width: 100%; margin: 0 auto; background: white; border-radius: 4px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); overflow: hidden; }");
            sb.AppendLine("        .header { background: #e9ecef; color: #333; padding: 8px 15px; display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid #dee2e6; }");
            sb.AppendLine("        .header h1 { font-size: 16px; font-weight: 600; }");
            sb.AppendLine("        .header p { font-size: 11px; color: #6c757d; }");
            
            // Контейнер для блоков таблиц
            sb.AppendLine("        .tables-wrapper { padding: 5px; overflow-x: auto; }");
            sb.AppendLine("        .tables-container { display: flex; gap: 15px; flex-wrap: wrap; }");
            sb.AppendLine("        .table-block { flex: 0 0 auto; border: 1px solid #dee2e6; border-radius: 4px; overflow: hidden; }");
            sb.AppendLine("        .block-header { font-size: 10px; font-weight: 600; padding: 4px 8px; background: #e9ecef; color: #495057; text-align: center; border-bottom: 1px solid #dee2e6; }");
            
            sb.AppendLine("        table { width: auto; border-collapse: collapse; font-size: 11px; }");
            sb.AppendLine("        th { background: #f8f9fa; color: #333; font-weight: 600; text-align: left; padding: 6px 5px; border-bottom: 2px solid #dee2e6; border-right: 1px solid #dee2e6; }");
            sb.AppendLine("        td { padding: 5px; border-bottom: 1px solid #dee2e6; border-right: 1px solid #dee2e6; }");
            sb.AppendLine("        tr:hover { background-color: #f8f9fa; }");
            sb.AppendLine("        th:first-child, td:first-child { width: 50px; min-width: 50px; max-width: 200px; }");
            sb.AppendLine("        th:not(:first-child), td:not(:first-child) { width: 100px; min-width: 50px; max-width: 200px; }");
            sb.AppendLine("        .acc-column { font-family: 'Consolas', monospace; font-weight: bold; background: #000; color: #fff; padding: 5px !important; }");
            sb.AppendLine("        .balance-cell { font-family: 'Consolas', monospace; font-weight: bold; text-align: right; font-size: 10px; }");
            sb.AppendLine("        .balance-highest { background-color: #4682B4; color: white; }");
            sb.AppendLine("        .balance-high { background-color: #228B22; color: white; }");
            sb.AppendLine("        .balance-medium { background-color: #9ACD32; }");
            sb.AppendLine("        .balance-low { background-color: #F0E68C; }");
            sb.AppendLine("        .balance-verylow { background-color: #FFA07A; }");
            sb.AppendLine("        .balance-minimal { background-color: #CD5C5C; color: white; }");
            sb.AppendLine("        .balance-zero { background-color: #fff; color: #fff; }");
            sb.AppendLine("        .summary-row { font-weight: bold; background: #e9ecef !important; border-top: 3px solid #333 !important; }");
            sb.AppendLine("        .summary-row td { padding: 7px 5px !important; font-size: 11px; }");
            
            sb.AppendLine("        .stats { display: flex; justify-content: space-around; padding: 8px; background: #f8f9fa; border-top: 1px solid #dee2e6; }");
            sb.AppendLine("        .stat-item { text-align: center; }");
            sb.AppendLine("        .stat-value { font-size: 16px; font-weight: bold; color: #333; }");
            sb.AppendLine("        .stat-label { font-size: 10px; color: #6c757d; margin-top: 2px; }");
            
            sb.AppendLine("        .pagination { display: flex; justify-content: center; align-items: center; gap: 5px; padding: 8px; background: #f8f9fa; border-top: 1px solid #dee2e6; }");
            sb.AppendLine("        .pagination button { padding: 5px 12px; background: #495057; color: white; border: none; border-radius: 3px; cursor: pointer; font-size: 11px; font-weight: 500; transition: background 0.2s; }");
            sb.AppendLine("        .pagination button:hover:not(:disabled) { background: #343a40; }");
            sb.AppendLine("        .pagination button:disabled { background: #ccc; cursor: not-allowed; opacity: 0.6; }");
            sb.AppendLine("        .pagination .page-info { font-size: 11px; color: #6c757d; min-width: 100px; text-align: center; }");
            
            sb.AppendLine("        .hidden { display: none; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            
            sb.AppendLine("<div class='container'>");
            sb.AppendLine("    <div class='header'>");
            sb.AppendLine("        <h1>Balance Report - Multi Column View</h1>");
            sb.AppendLine($"        <p>{DateTime.Now:dd.MM.yyyy HH:mm} | Accounts: {rows.Length} | Blocks: {(int)Math.Ceiling((double)rows.Length / rowsPerBlock)}</p>");
            sb.AppendLine("    </div>");
            
            // Разбиваем строки на блоки
            var blocks = new List<List<string>>();
            for (int i = 0; i < rows.Length; i += rowsPerBlock)
            {
                var block = rows.Skip(i).Take(rowsPerBlock).ToList();
                blocks.Add(block);
            }
            
            sb.AppendLine("    <div class='tables-wrapper'>");
            sb.AppendLine("        <div class='tables-container'>");
            
            // Создаём таблицу для каждого блока
            for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
            {
                var blockRows = blocks[blockIndex];
                int startRow = blockIndex * rowsPerBlock + 1;
                int endRow = startRow + blockRows.Count - 1;
                
                sb.AppendLine($"        <div class='table-block' data-block='{blockIndex}'>");
                sb.AppendLine($"            <div class='block-header'>Rows {startRow}-{endRow}</div>");
                sb.AppendLine("            <table>");
                
                // Заголовки
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
                
                // Строки данных для этого блока
                foreach (var row in blockRows)
                {
                    var values = row.Split('|');
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
                            decimal balance = 0;
                            
                            if (decimal.TryParse(val, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out balance))
                            {
                                columnSums[i] += balance;
                                rowSum += balance;
                            }
                            
                            string cssClass = GetBalanceCssClass(balance);
                            string formatted = FormatBalance(balance);
                            sb.AppendLine($"                    <td class='balance-cell {cssClass}'>{formatted}</td>");
                        }
                    }
                    
                    blockTotal += rowSum;
                    string rowSumClass = GetBalanceCssClass(rowSum);
                    sb.AppendLine($"                    <td class='balance-cell {rowSumClass}'>{FormatBalance(rowSum)}</td>");
                    sb.AppendLine("                </tr>");
                }
                
                // Итоговая строка для блока
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
            
            // Пагинация
            sb.AppendLine("    <div class='pagination'>");
            sb.AppendLine("        <button id='firstBtn' onclick='goToPage(1)'>⏮</button>");
            sb.AppendLine("        <button id='prevBtn' onclick='goToPage(currentPage - 1)'>←</button>");
            sb.AppendLine("        <span class='page-info' id='pageInfo'>Page 1</span>");
            sb.AppendLine("        <button id='nextBtn' onclick='goToPage(currentPage + 1)'>→</button>");
            sb.AppendLine("        <button id='lastBtn' onclick='goToPage(totalPages)'>⏭</button>");
            sb.AppendLine("    </div>");
            
            // Статистика
            int accountsWithBalance = 0;
            int accountsAbove01 = 0;
            decimal grandTotal = 0;
            
            foreach (var row in rows)
            {
                var values = row.Split('|');
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
            sb.AppendLine("        <div class='stat-item'>");
            sb.AppendLine($"            <div class='stat-value'>{rows.Length}</div>");
            sb.AppendLine("            <div class='stat-label'>Total</div>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class='stat-item'>");
            sb.AppendLine($"            <div class='stat-value'>{accountsWithBalance}</div>");
            sb.AppendLine("            <div class='stat-label'>Active</div>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class='stat-item'>");
            sb.AppendLine($"            <div class='stat-value'>{accountsAbove01}</div>");
            sb.AppendLine("            <div class='stat-label'>≥ 0.1</div>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class='stat-item'>");
            sb.AppendLine($"            <div class='stat-value'>{FormatBalance(grandTotal)}</div>");
            sb.AppendLine("            <div class='stat-label'>Grand Total</div>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</div>");
            
            // JavaScript
            sb.AppendLine("<script>");
            sb.AppendLine($"    const blocksPerPage = {blocksPerPage};");
            sb.AppendLine("    let currentPage = 1;");
            sb.AppendLine("    const tableBlocks = document.querySelectorAll('.table-block');");
            sb.AppendLine("    const totalPages = Math.ceil(tableBlocks.length / blocksPerPage);");
            
            sb.AppendLine("    function showPage(page) {");
            sb.AppendLine("        const start = (page - 1) * blocksPerPage;");
            sb.AppendLine("        const end = start + blocksPerPage;");
            sb.AppendLine("        tableBlocks.forEach((block, index) => {");
            sb.AppendLine("            block.classList.toggle('hidden', index < start || index >= end);");
            sb.AppendLine("        });");
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
