using System;
using System.Collections.Generic;
using z3nCore.Utilities;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.Examples
{
    /// <summary>
    /// Примеры использования класса SocialReport
    /// </summary>
    public static class SocialReportExample
    {
        /// <summary>
        /// Пример 1: Использование СТАРОГО формата данных (ваш текущий подход)
        /// </summary>
        public static void Example1_OldFormat(IZennoPosterProjectModel project)
        {
            // Старый формат данных из вашего кода
            var acc0 = project.Int("rangeStart") - 1;
            var dataList = new List<Dictionary<string, string[]>>();

            while (acc0 < project.Int("rangeEnd"))
            {
                acc0++;
                project.Var("acc0", acc0);

                var twitter = project.DbGetColumns("status, login", "_twitter", log: true);
                var github = project.DbGetColumns("status, login", "_github", log: true);
                var discord = project.DbGetColumns("status, username", "_discord", log: true);
                var telegram = project.DbGetColumns("username", "_telegram", log: true);

                var data = new Dictionary<string, string[]>();
                if (twitter.ContainsKey("login"))
                    data.Add("twitter", new[] { twitter["status"], twitter["login"] });
                if (github.ContainsKey("login"))
                    data.Add("github", new[] { github["status"], github["login"] });
                if (discord.ContainsKey("username"))
                    data.Add("discord", new[] { discord["status"], discord["username"] });
                if (telegram.ContainsKey("username"))
                    data.Add("telegram", new[] { telegram["username"] });

                dataList.Add(data);
            }

            // Генерируем отчет из старого формата
            project.GenerateSocialReportFromOldFormat(dataList, call: true);
        }

        /// <summary>
        /// Пример 2: Использование НОВОГО формата данных (рекомендуется)
        /// Более эффективный и читаемый подход
        /// </summary>
        public static void Example2_NewFormat(IZennoPosterProjectModel project)
        {
            var accounts = new List<AccountSocialData>();

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
                        Status = "ok", // Telegram не возвращает статус
                        Login = telegram["username"]
                    };
                }

                accounts.Add(account);
            }

            // Генерируем отчет из нового формата
            project.GenerateSocialReport(accounts, call: true);
        }

        /// <summary>
        /// Пример 3: Использование напрямую через класс SocialReport
        /// </summary>
        public static void Example3_DirectUsage(IZennoPosterProjectModel project)
        {
            var socialReport = new SocialReport(project, log: true);

            // Создаем тестовые данные
            var accounts = new List<AccountSocialData>
            {
                new AccountSocialData(1)
                {
                    Twitter = new SocialStatus { Status = "ok", Login = "@user1" },
                    GitHub = new SocialStatus { Status = "ok", Login = "user1" },
                    Discord = new SocialStatus { Status = "banned", Login = "user1#1234" },
                    Telegram = new SocialStatus { Status = "ok", Login = "user1_tg" }
                },
                new AccountSocialData(2)
                {
                    Twitter = new SocialStatus { Status = "ok", Login = "@user2" },
                    Discord = new SocialStatus { Status = "ok", Login = "user2#5678" }
                    // GitHub и Telegram отсутствуют
                },
                new AccountSocialData(3)
                {
                    GitHub = new SocialStatus { Status = "suspended", Login = "user3" }
                    // Только GitHub
                }
            };

            // Генерируем отчет
            socialReport.ShowSocialTable(accounts, call: true);
        }

        /// <summary>
        /// Пример 4: Конвертация из старого формата в новый
        /// Полезно если вы хотите работать с новым форматом, но у вас уже есть старые данные
        /// </summary>
        public static void Example4_Conversion(IZennoPosterProjectModel project)
        {
            // Получаем данные в старом формате
            var oldFormatData = new List<Dictionary<string, string[]>>
            {
                new Dictionary<string, string[]>
                {
                    { "twitter", new[] { "ok", "@alice" } },
                    { "github", new[] { "ok", "alice" } }
                },
                new Dictionary<string, string[]>
                {
                    { "discord", new[] { "ok", "bob#1234" } },
                    { "telegram", new[] { "bob_tg" } }
                }
            };

            // Конвертируем в новый формат
            var newFormatData = SocialReport.ConvertFromOldFormat(oldFormatData);

            // Теперь можем работать с новым форматом
            foreach (var account in newFormatData)
            {
                project.SendInfoToLog($"Account {account.AccountId}:", false);
                if (account.Twitter?.IsActive == true)
                    project.SendInfoToLog($"  Twitter: {account.Twitter.Login} ({account.Twitter.Status})", false);
                if (account.GitHub?.IsActive == true)
                    project.SendInfoToLog($"  GitHub: {account.GitHub.Login} ({account.GitHub.Status})", false);
                if (account.Discord?.IsActive == true)
                    project.SendInfoToLog($"  Discord: {account.Discord.Login} ({account.Discord.Status})", false);
                if (account.Telegram?.IsActive == true)
                    project.SendInfoToLog($"  Telegram: {account.Telegram.Login}", false);
            }

            // Генерируем отчет
            project.GenerateSocialReport(newFormatData, call: true);
        }
    }
}
