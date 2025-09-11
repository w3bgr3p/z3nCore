using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

using Newtonsoft.Json;

namespace z3nCore.Socials
{
    public class Galxe

    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly bool _log;

        private readonly string GRAPHQL_URL = "https://graphigo.prd.galaxy.eco/query";

        public Galxe(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _log = log;
        }

        public List<HtmlElement> ParseTasks(string type = "tasksUnComplete", bool log = false) //tasksComplete|tasksUnComplete|reqComplete|reqUnComplete|refComplete|refUnComplete
        {
            string sectionName = null;
            var reqComplete = new List<HtmlElement>();
            var reqUnComplete = new List<HtmlElement>();

            var tasksComplete = new List<HtmlElement>();
            var tasksUnComplete = new List<HtmlElement>();

            var refComplete = new List<HtmlElement>();
            var refUnComplete = new List<HtmlElement>();

            var dDone = "M10 19a9 9 0 1 0 0-18 9 9 0 0 0 0 18m3.924-10.576a.6.6 0 0 0-.848-.848L9 11.652 6.924 9.575a.6.6 0 0 0-.848.848l2.5 2.5a.6.6 0 0 0 .848 0z";

            var sectionList = _instance.ActiveTab.FindElementByAttribute("div", "class", "mb-20", "regexp", 0).GetChildren(false).ToList();

            foreach (HtmlElement section in sectionList)
            {
                sectionName = null;
                var taskList = section.GetChildren(false).ToList();
                foreach (HtmlElement taskTile in taskList)
                {
                    if (taskTile.GetAttribute("class") == "flex justify-between")
                    {
                        sectionName = taskTile.InnerText.Replace("\n", " ");
                        _project.SendInfoToLog(sectionName);
                        continue;
                    }
                    if (sectionName.Contains("Requirements"))
                    {
                        var taskText = taskTile.InnerText.Replace("View Detail", "").Replace("\n", ";");
                        if (taskTile.InnerHtml.Contains(dDone)) reqComplete.Add(taskTile);
                        else reqUnComplete.Add(taskTile);
                    }
                    else if (sectionName.Contains("Get") && !sectionName.Contains("Referral"))
                    {
                        var taskText = taskTile.InnerText.Replace("View Detail", "").Replace("\n", ";");
                        if (taskTile.InnerHtml.Contains(dDone)) tasksComplete.Add(taskTile);
                        else tasksUnComplete.Add(taskTile);
                    }
                    else if (sectionName.Contains("Referral"))
                    {
                        var taskText = taskTile.InnerText.Replace("View Detail", "").Replace("\n", ";");
                        if (taskTile.InnerHtml.Contains(dDone)) refComplete.Add(taskTile);
                        else refUnComplete.Add(taskTile);
                    }

                }
            }

            _project.SendInfoToLog($"requirements done/!done {reqComplete.Count}/{reqUnComplete.Count}");
            _project.SendInfoToLog($"tasks done/!done {tasksComplete.Count}/{tasksUnComplete.Count}");
            _project.SendInfoToLog($"refs counted {refComplete.Count}");

            switch (type) //tasksComplete|tasksUnComplete|reqComplete|reqUnComplete|refComplete|refUnComplete
            {
                case "tasksComplete": return tasksComplete;
                case "tasksUnComplete": return tasksUnComplete;
                case "reqComplete": return reqComplete;
                case "reqUnComplete": return reqUnComplete;
                case "refComplete": return refComplete;
                case "refUnComplete": return refUnComplete;
                default: return tasksUnComplete;
            }

        }

        public string BasicUserInfo(string token, string address)
        {
            // GraphQL-запрос с исправленным полем injectiveAddress
            string query = @"
					query BasicUserInfo($address: String!) {
						addressInfo(address: $address) {
							id
							username
							address
							evmAddressSecondary {
								address
								__typename
							}
							userLevel {
								level {
									name
									logo
									minExp
									maxExp
									__typename
								}
								exp
								gold
								__typename
							}
							ggInviteeInfo {
								questCount
								ggCount
								__typename
							}
							ggInviteCode
							ggInviter {
								id
								username
								__typename
							}
							isBot
							solanaAddress
							aptosAddress
							starknetAddress
							bitcoinAddress
							suiAddress
							xrplAddress
							tonAddress
							displayNamePref
							email
							twitterUserID
							twitterUserName
							githubUserID
							githubUserName
							discordUserID
							discordUserName
							telegramUserID
							telegramUserName
							enableEmailSubs
							subscriptions
							isWhitelisted
							isInvited
							isAdmin
							accessToken
							humanityType
							participatedCampaigns {
								totalCount
								__typename
							}
							__typename
						}
					}";

            // Переменные для запроса с динамическим адресом
            string variables = $"{{\"address\": \"EVM:{address}\"}}";

            // Проверка токена
            if (string.IsNullOrEmpty(token))
            {
                _project.SendErrorToLog("Token is empty or null");
                return null;
            }
            //token = null;
            // Формируем заголовки (только необходимые)
            string[] headers = new string[]
            {
                    "Content-Type: application/json",
                    $"Authorization: {token}"
            };

            // Форматируем запрос (удаляем лишние пробелы и переносы строк)
            query = query.Replace("\t", "").Replace("\n", " ").Replace("\r", "").Trim();

            // Формируем тело запроса
            string jsonBody = JsonConvert.SerializeObject(new
            {
                operationName = "BasicUserInfo",
                query = query,
                variables = JsonConvert.DeserializeObject(variables)
            });

            _project.SendInfoToLog($"Request headers: {string.Join(", ", headers)}");
            _project.SendInfoToLog($"Request body: {jsonBody}");

            try
            {
	            /*
                string response = ZennoPoster.HttpPost(
                    GRAPHQL_URL,
                    Encoding.UTF8.GetBytes(jsonBody),
                    "application/json",
                    _project.Variables["proxy"].Value,
                    "UTF-8",
                    ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.BodyOnly,
                    30000,
                    "",
                    "Galaxy/v1",
                    true,
                    5,
                    headers,
                    "",
                    true
                );
                */
                string result = ZennoPoster.HTTP.Request(
	                ZennoLab.InterfacesLibrary.Enums.Http.HttpMethod.POST,
	                GRAPHQL_URL,
	                Encoding.UTF8.GetBytes(jsonBody),
	                "application/json",
	                _project.Variables["proxy"].Value,
	                "UTF-8",
	                ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.BodyOnly,
	                30000,
	                "",
	                "Galaxy/v1",
	                true,
	                5,
	                headers,
	                "",
	                true,
	                false,
	                null);
                _project.SendInfoToLog($"Response received: {result.Substring(0, Math.Min(100, result.Length))}...");
                _project.Json.FromString(result);
                return result;
            }
            catch (Exception ex)
            {
                _project.SendErrorToLog($"GraphQL request failed: {ex.Message}");
                return null;
            }
        }

        public string GetLoyaltyPoints(string alias, string address)
        {
            // GraphQL-запрос SpaceAccessQuery
            string query = @"
				query SpaceAccessQuery($id: Int, $alias: String, $address: String!) {
					space(id: $id, alias: $alias) {
						id
						addressLoyaltyPoints(address: $address) {
							points
							rank
							__typename
						}
						__typename
					}
				}";

            // Переменные для запроса
            string variables = $"{{\"alias\": \"{alias}\", \"address\": \"{address.ToLower()}\"}}";

            // Формируем заголовки (аналогично Google Apps Script)
            string[] headers = new string[]
            {
                "Content-Type: application/json",
                "Accept: */*",
                "Authority: graphigo.prd.galaxy.eco",
                "Origin: https://galxe.com",
                "User-Agent: Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36"
            };

            // Форматируем запрос (удаляем лишние пробелы и переносы строк)
            query = query.Replace("\t", "").Replace("\n", " ").Replace("\r", "").Trim();

            // Формируем тело запроса
            string jsonBody = JsonConvert.SerializeObject(new
            {
                operationName = "SpaceAccessQuery",
                query = query,
                variables = JsonConvert.DeserializeObject(variables)
            });

            _project.SendInfoToLog($"Request headers: {string.Join(", ", headers)}");
            _project.SendInfoToLog($"Request body: {jsonBody}");

            try
            {
	            /*
                string response = ZennoPoster.HttpPost(
                    "https://graphigo.prd.galaxy.eco/query", // URL эндпоинта
                    Encoding.UTF8.GetBytes(jsonBody),
                    "application/json",
                    _project.Variables["proxy"].Value,
                    "UTF-8",
                    ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.BodyOnly,
                    30000,
                    "",
                    "Galaxy/v1",
                    true,
                    5,
                    headers,
                    "",
                    true
                );
                */
                string result = ZennoPoster.HTTP.Request(
	                ZennoLab.InterfacesLibrary.Enums.Http.HttpMethod.POST,
	                "https://graphigo.prd.galaxy.eco/query",
	                Encoding.UTF8.GetBytes(jsonBody),
	                "application/json",
	                _project.Variables["proxy"].Value,
	                "UTF-8",
	                ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.BodyOnly,
	                30000,
	                "",
	                "Galaxy/v1",
	                true,
	                5,
	                headers,
	                "",
	                true,
	                false,
	                null);
                _project.SendInfoToLog($"Response received: {result.Substring(0, Math.Min(100, result.Length))}...");
                _project.Json.FromString(result);
                return result;
            }
            catch (Exception ex)
            {
                _project.SendErrorToLog($"GraphQL request failed: {ex.Message}");
                return null;
            }
        }

    }
}
