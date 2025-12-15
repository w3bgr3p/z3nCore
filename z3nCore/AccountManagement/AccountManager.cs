using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using ZennoLab.InterfacesLibrary.Enums.Log;


namespace z3nCore
{
    public static class AccountManager
    {
        private static List<string> ParseRangeGroups(IZennoPosterProjectModel project, string cfgAccRange)
        {
            var groups = new List<string>();
    
            // Сплитим по : (группы приоритета)
            foreach (var groupRange in cfgAccRange.Split(':'))
            {
                // Каждую группу обрабатываем через оригинальный Range()
                var rangeIds = project.Range(groupRange); // вернет List<string> ID
                // Склеиваем обратно в строку для SQL IN (...)
                groups.Add(string.Join(",", rangeIds));
            }
    
            return groups;
        }
        
        private static string ParseSingleRange(string rangeStr)
        {
            if (rangeStr.Contains(","))
            {
                // Already comma-separated list
                return rangeStr;
            }
            else if (rangeStr.Contains("-"))
            {
                // Range format like "1-100"
                var parts = rangeStr.Split('-').Select(int.Parse).ToArray();
                int start = parts[0];
                int end = parts[1];
                return string.Join(",", Enumerable.Range(start, end - start + 1));
            }
            else
            {
                // Single number
                return rangeStr;
            }
        }

        private static void GetListFromDb(this IZennoPosterProjectModel project,
            string condition,
            string sortByTaskAge = null,
            bool useRange = true,
            bool filterTwitter = false,
            bool filterDiscord = false,
            string tableName = null,
            bool debugLog = false)
        {
            if (!string.IsNullOrEmpty(project.Var("acc0")))
            {
                return;
            }
            if (string.IsNullOrEmpty(tableName)) 
                tableName = project.ProjectTable();
            
            // Parse priority groups from cfgAccRange
            List<string> rangeGroups = new List<string>();
            if (useRange)
            {
                var cfgAccRange = project.Var("cfgAccRange");
                rangeGroups = ParseRangeGroups(project, cfgAccRange);
            }
            else
            {
                rangeGroups.Add(null); // Single iteration without range
            }
            
            // Try each priority group in order
            foreach (var rangeGroup in rangeGroups)
            {
                var fullCondition = useRange
                    ? $"{condition} AND id in ({rangeGroup})"
                    : condition;

                List<string> accounts;

                if (!string.IsNullOrEmpty(sortByTaskAge))
                {
                    var selectColumns = $"id, {sortByTaskAge}";
                    var orderBy = $"CASE WHEN {sortByTaskAge} = '' OR {sortByTaskAge} IS NULL THEN '9999-12-31' ELSE {sortByTaskAge} END ASC";
                    var query = $"SELECT {selectColumns} FROM {tableName} WHERE {fullCondition} ORDER BY {orderBy}";

                    var rawData = project.DbQ(query, log: debugLog);

                    var accountsWithDates = rawData.Split('·')
                        .Where(row => !string.IsNullOrWhiteSpace(row))
                        .Select(row =>
                        {
                            var parts = row.Split('|');
                            var id = parts[0];
                            var dateStr = parts.Length > 1 ? parts[1] : "";

                            DateTime date;
                            if (!DateTime.TryParseExact(dateStr,
                                    "yyyy-MM-ddTHH:mm:ss.fffZ",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                    out date))
                            {
                                date = DateTime.MaxValue;
                            }

                            return new { Id = id, Date = date };
                        })
                        .OrderBy(x => x.Date)
                        .Select(x => x.Id)
                        .ToList();

                    accounts = accountsWithDates;
                }
                else
                {
                    accounts = project.DbGetLines("id",tableName: tableName, log: debugLog, where: fullCondition)
                        .Where(acc => !string.IsNullOrWhiteSpace(acc))
                        .ToList();
                }

                if (!accounts.Any())
                {
                    // No accounts found in this group, try next priority group
                    continue;
                }

                project.ListSync("accs", accounts);

                if (filterTwitter)
                {
                    if (!project.FilterBySocial("twitter", condition))
                        continue; // Try next group
                }

                if (filterDiscord)
                {
                    if (!project.FilterBySocial("discord", condition))
                        continue; // Try next group
                }

                accounts = project.Lists["accs"].ToList();

                if (!accounts.Any())
                {
                    // After filtering, no accounts left, try next group
                    continue;
                }
                
                // Successfully found accounts, exit
                return;
            }
            
            project.warn($"No accounts found by condition\n{condition.Replace("\n", " ")} in range {project.Var("cfgAccRange")}");
        }

        public static void ChooseAccountByCondition(this IZennoPosterProjectModel project,
            string condition,
            string sortByTaskAge = null,
            bool useRange = true,
            bool filterTwitter = false,
            bool filterDiscord = false,
            string tableName = null,
            bool debugLog = false)
        {
            if (!string.IsNullOrEmpty(project.Var("acc0Forced")))
            {
                project.Var("acc0", project.Var("acc0Forced"));
                return;
            }

            // Get filtered list from DB
            project.GetListFromDb(condition, sortByTaskAge, useRange, filterTwitter, filterDiscord,tableName ,debugLog);

            var accounts = project.Lists["accs"].ToList();

            if (!accounts.Any())
            {
                project.warn($"Account selection failed: condition={condition}, found=0 in all priority groups", true);
                return;
            }

            var acc0 = !string.IsNullOrEmpty(sortByTaskAge)
                ? accounts.First()
                : project.RndFromList("accs", true);

            project.Var("acc0", acc0);

            if (!string.IsNullOrEmpty(sortByTaskAge))
                project.Lists["accs"].Remove(acc0);

            var left = project.Lists["accs"].Count;
            project.DbUpd($"status = 'working...'");
            project.SendToLog($"Account selected: acc={acc0}, remaining={left}, condition={condition}, range={project.Var("cfgAccRange")}", LogType.Info, true, LogColor.Gray);
        }

        public static void ChooseAndRunByCondition(this IZennoPosterProjectModel project,Instance instance, 
            string condition, 
            bool browser = false,
            string sortByTaskAge = null, 
            bool useRange = true, 
            bool filterTwitter = false, 
            bool filterDiscord = false, 
            string tableName = null, 
            bool debugLog = false)
        {
            
            while (true)
            {
                try{
                    project.ChooseAccountByCondition(condition,sortByTaskAge,useRange,filterTwitter,filterDiscord,tableName,debugLog); 

                    if (string.IsNullOrEmpty(project.Var("acc0"))) 
                        throw new Exception("");
                    
                    var browserMode = browser ? "Chromium" : "WithoutBrowser";
                    //run
                    project.RunBrowser(instance,browserMode);
                    return;
		
                }
                catch (Exception ex)
                {

                    bool thrw = !ex.Message.Contains("Браузер не может быть запущен в указанной папке");
                    
                    project.warn(ex,thrw);
                    continue;
                }

            }


        }

        private static bool FilterBySocial(this IZennoPosterProjectModel project, string socialName, string originalCondition = "")
        {
            var accs = project.ListSync("accs");
            var filtered = new HashSet<string>(
                project.DbGetLines("id", $"_{socialName.ToLower()}", where: @"status = 'ok'")
            );

            var combined = accs
                .Where(acc => filtered.Contains(acc))
                .ToList();
            
            project.ListSync("accs", combined);
            
            if (!combined.Any())
            {
                var conditionInfo = string.IsNullOrEmpty(originalCondition) ? "" : $", condition={originalCondition}";
                project.warn($"Social filter failed: social={socialName}, found=0{conditionInfo}");
                return false;
            }
            return true;
        }
    }

}