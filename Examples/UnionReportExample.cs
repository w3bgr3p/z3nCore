using System.Collections.Generic;
using z3nCore.Utilities;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.Examples
{
    /// <summary>
    /// Примеры использования UnionReport - объединённого отчёта
    /// </summary>
    public static class UnionReportExample
    {
        /// <summary>
        /// Полный пример: генерация объединённого отчёта Social + Daily
        /// </summary>
        public static void GenerateFullUnionReport(IZennoPosterProjectModel project)
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

        /// <summary>
        /// Упрощённый пример: если данные уже собраны отдельно
        /// </summary>
        public static void GenerateUnionFromExistingData(
            IZennoPosterProjectModel project,
            List<AccountSocialData> socialAccounts,
            List<DailyReport.ProjectData> dailyProjects)
        {
            // Просто вызываем метод с готовыми данными
            project.GenerateUnionReport(socialAccounts, dailyProjects, call: true);
        }

        /// <summary>
        /// Пример: генерация только если есть данные
        /// </summary>
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
