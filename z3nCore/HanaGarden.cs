using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


namespace z3nCore
{
    public static class HanaGarden
    {
        private static readonly string GRAPHQL_URL = "https://hanafuda-backend-app-520478841386.us-central1.run.app/graphql";
        private static readonly string API_KEY = "AIzaSyDipzN0VRfTPnMGhQ5PSzO27Cxm3DohJGY";

        private static string ExecuteGraphQLQuery(IZennoPosterProjectModel project, string query, string variables = null)
        {
            // Получаем токен и проверяем его
            string token = project.Variables["TOKEN_CURRENT"].Value.Trim();

            if (string.IsNullOrEmpty(token))
            {
                project.SendErrorToLog("Token is empty or null");
                return null;
            }

            // Форматируем заголовки, убедившись что токен передается корректно
            string[] headers = new string[] {
                "Content-Type: application/json",
                $"Authorization: Bearer {token.Trim()}"
            };

            // Форматируем GraphQL запрос, удаляя лишние пробелы и табуляции
            query = query.Replace("\t", "").Replace("\n", " ").Replace("\r", "").Trim();

            //string jsonBody = JsonConvert.SerializeObject(new { query = query });
            string jsonBody;
            if (variables != null)
            {
                jsonBody = JsonConvert.SerializeObject(new { query = query, variables = JsonConvert.DeserializeObject(variables) });
            }
            else
            {
                jsonBody = JsonConvert.SerializeObject(new { query = query });
            }




            try
            {
                string response = ZennoPoster.HttpPost(
                    GRAPHQL_URL,
                    Encoding.UTF8.GetBytes(jsonBody),
                    "application/json",
                    project.Variables["proxy"].Value,
                    "UTF-8",
                    ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.BodyOnly,
                    30000,
                    "",
                    "HANA/v1",
                    true,
                    5,
                    headers,
                    "",
                    true
                );

                return response;
            }
            catch (Exception ex)
            {
                project.SendErrorToLog($"GraphQL request failed: {ex.Message}");
                return null;
            }
        }
        public static string RefreshToken(IZennoPosterProjectModel project, string currentToken)
        {
            string url = $"https://securetoken.googleapis.com/v1/token?key={API_KEY}";

            string jsonBody = JsonConvert.SerializeObject(new
            {
                grant_type = "refresh_token",
                refresh_token = currentToken
            });


            string[] headers = new string[] {
                "Content-Type: application/json"
            };

            try
            {
                string response = ZennoPoster.HttpPost(
                    url,
                    Encoding.UTF8.GetBytes(jsonBody),
                    "application/json",
                    project.Variables["proxy"].Value,
                    "UTF-8",
                    ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.BodyOnly,
                    30000,
                    "",
                    "Firebase/v1",
                    true,
                    5,
                    headers,
                    "",
                    true
                );


                dynamic tokenData = JObject.Parse(response);
                string newToken = tokenData.access_token;

                // Сохраняем новый токен в переменную проекта
                project.Variables["TOKEN_CURRENT"].Value = newToken;

                return newToken;
            }
            catch (Exception ex)
            {
                project.SendErrorToLog($"Failed to refresh token: {ex.Message}");
                return null;
            }
        }




        private static dynamic GetUserInfo(IZennoPosterProjectModel project)
        {
            string query = @"
			query CurrentUser {
				currentUser {
					id
					sub
					name
					totalPoint
					evmAddress {
						userId
						address
					}
				}
			}";

            string response = ExecuteGraphQLQuery(project, query);
            return JObject.Parse(response);
        }// Получение информации о картах пользователя
        public static string GetUserYakuInfo(IZennoPosterProjectModel project)
        {
            string query = @"
			query GetYakuList {
				getYakuListForCurrentUser {
					cardId
					group
				}
			}";

            return ExecuteGraphQLQuery(project, query);
        }
        public static string GetUserYakuInfo2(IZennoPosterProjectModel project)
        {
            string query = @"
			query GetMasterData {
				masterData {
					yaku {
					cardId
					group
					}
				}
			}";

            return ExecuteGraphQLQuery(project, query);
        }

        // Получение информации о саде
        public static string GetGardenInfo(IZennoPosterProjectModel project)
        {
            project.SendInfoToLog("Getting garden info...");
            string query = @"
			query GetGardenForCurrentUser {
				getGardenForCurrentUser {
					id
					inviteCode
					gardenDepositCount
					gardenStatus {
						id
						activeEpoch
						growActionCount
						gardenRewardActionCount
					}
					gardenMembers {
						id
						sub
						name
						iconPath
						depositCount
					}
				}
			}";

            return ExecuteGraphQLQuery(project, query);
        }

        public static void ProcessGarden(IZennoPosterProjectModel project)
        {
            try
            {
                // Получаем и обновляем токен
                string currentToken = project.Variables["TOKEN_CURRENT"].Value;
                project.SendInfoToLog($"Initial token: {currentToken}");

                string refreshedToken = RefreshToken(project, currentToken);
                if (string.IsNullOrEmpty(refreshedToken))
                {
                    project.SendErrorToLog("Failed to refresh token");
                    return;
                }

                project.SendInfoToLog($"Successfully refreshed token: {refreshedToken}");

                // Получаем информацию о саде
                project.SendInfoToLog("Getting garden info...");
                string gardenResponse = ExecuteGraphQLQuery(project, @"
					query GetGardenForCurrentUser {
						getGardenForCurrentUser {
							id
							inviteCode
							gardenDepositCount
							gardenStatus {
								id
								activeEpoch
								growActionCount
								gardenRewardActionCount
							}
							gardenMembers {
								id
								sub
								name
								iconPath
								depositCount
							}
						}
					}");

                project.SendInfoToLog($"Garden response received: {gardenResponse.Substring(0, Math.Min(100, gardenResponse.Length))}...");

                if (string.IsNullOrEmpty(gardenResponse))
                {
                    project.SendErrorToLog("Garden response is empty!");
                    return;
                }

                dynamic gardenData = JObject.Parse(gardenResponse);

                if (gardenData.data == null || gardenData.data.getGardenForCurrentUser == null)
                {
                    project.SendErrorToLog($"Invalid garden data structure: {gardenResponse}");
                    return;
                }

                dynamic gardenStatus = gardenData.data.getGardenForCurrentUser.gardenStatus;
                dynamic gardenMembers = gardenData.data.getGardenForCurrentUser.gardenMembers;

                // Проверяем наличие необходимых данных
                if (gardenStatus == null)
                {
                    project.SendErrorToLog("Garden status is null!");
                    return;
                }

                int totalGrows = (int)gardenStatus.growActionCount;
                int totalRewards = (int)gardenStatus.gardenRewardActionCount;

                project.SendInfoToLog($"Found actions - Grows: {totalGrows}, Rewards: {totalRewards}");

                string accountName = "Unknown";
                string accountId = "Unknown";

                if (gardenMembers != null && gardenMembers.Count > 0)
                {
                    accountName = gardenMembers[0].name;
                    accountId = gardenMembers[0].id;
                }

                project.SendInfoToLog($"Processing account: {accountName} (ID: {accountId})");



                //grow
                string growQuery = @"
				mutation {
					executeGrowAction(withAll: true) {
						baseValue
						leveragedValue
						totalValue
						multiplyRate
						limit
					}
				}";

                project.SendInfoToLog($"Executing grow all action");
                string growResponse = ExecuteGraphQLQuery(project, growQuery);
                project.SendInfoToLog($"Grow response: {growResponse}");

                dynamic growData = JObject.Parse(growResponse);
                if (growData.data != null && growData.data.executeGrowAction != null)
                {
                    var result = growData.data.executeGrowAction;
                    project.SendInfoToLog($"Grow results: Base={result.baseValue}, " +
                                        $"Leveraged={result.leveragedValue}, " +
                                        $"Total={result.totalValue}, " +
                                        $"Rate={result.multiplyRate}, " +
                                        $"Limit={result.limit}");
                }


                // Получаем обновленные очки
                string userInfoResponse = ExecuteGraphQLQuery(project, @"
					query CurrentUser {
						currentUser {
							totalPoint
						}
					}");

                dynamic userInfo = JObject.Parse(userInfoResponse);
                int totalPoints = (int)userInfo.data.currentUser.totalPoint;

                project.SendInfoToLog($"Grow action completed. Current Total Points: {totalPoints}");

                int delay = new Random().Next(1000, 5000);
                project.SendInfoToLog($"Waiting for {delay}ms before next action");
                Thread.Sleep(delay);


                // Получение наград
                if (totalRewards > 0)
                {
                    project.SendInfoToLog($"Starting reward collection. Total rewards: {totalRewards}");

                    string rewardQuery = @"
					mutation executeGardenRewardAction($limit: Int!) {
						executeGardenRewardAction(limit: $limit) {
							data { cardId, group }
							isNew
						}
					}";

                    int steps = (int)Math.Ceiling(totalRewards / 10.0);
                    project.SendInfoToLog($"Will process rewards in {steps} steps");

                    for (int i = 0; i < steps; i++)
                    {
                        try
                        {
                            project.SendInfoToLog($"Processing rewards step {i + 1} of {steps}");
                            string variables = @"{""limit"": 10}";
                            string rewardResponse = ExecuteGraphQLQuery(project, rewardQuery, variables);
                            project.SendInfoToLog($"Reward response: {rewardResponse}");

                            dynamic rewardData = JObject.Parse(rewardResponse);

                            foreach (var reward in rewardData.data.executeGardenRewardAction)
                            {
                                if ((bool)reward.isNew)
                                {
                                    project.SendInfoToLog($"New card received: ID {reward.data.cardId}, Group: {reward.data.group}");
                                }
                            }

                            delay = new Random().Next(1000, 5000);
                            project.SendInfoToLog($"Waiting for {delay}ms before next reward collection");
                            Thread.Sleep(delay);
                        }
                        catch (Exception ex)
                        {
                            project.SendErrorToLog($"Error during reward collection: {ex.Message}\nStack trace: {ex.StackTrace}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                project.SendErrorToLog($"Major error in garden processing: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }


        // Выполнение всех доступных действий роста
        public static string ExecuteGrowAll(IZennoPosterProjectModel project)
        {
            string query = @"
			mutation {
				executeGrowAction(withAll: true) {
					baseValue
					leveragedValue
					totalValue
					multiplyRate
					limit
				}
			}";

            return ExecuteGraphQLQuery(project, query);
        }

        // Получение текущих очков пользователя
        public static string GetUserPoints(IZennoPosterProjectModel project)
        {
            string query = @"
			query CurrentUser {
				currentUser {
					totalPoint
				}
			}";

            return ExecuteGraphQLQuery(project, query);
        }

        // Получение наград с указанным лимитом
        public static string CollectRewards(IZennoPosterProjectModel project, int limit)
        {
            string query = @"
			mutation executeGardenRewardAction($limit: Int!) {
				executeGardenRewardAction(limit: $limit) {
					data { 
						cardId
						group 
					}
					isNew
				}
			}";

            string variables = $"{{\"limit\": {limit}}}";
            return ExecuteGraphQLQuery(project, query, variables);
        }








    }
    public static class HanaAPI
    {
        private static readonly string GRAPHQL_URL = "https://hanafuda-backend-app-520478841386.us-central1.run.app/graphql";

        public static string GetSchemaInfo(IZennoPosterProjectModel project)
        {
            string introspectionQuery = @"
			query {
				__schema {
					types {
						name
						fields {
							name
							type {
								name
								kind
							}
						}
					}
					mutationType {
						fields {
							name
							type {
								name
							}
							args {
								name
								type {
									name
								}
							}
						}
					}
				}
			}";

            string[] headers = new string[] {
                "Content-Type: application/json",
                $"Authorization: Bearer {project.Variables["TOKEN_CURRENT"].Value}"
            };

            string jsonBody = JsonConvert.SerializeObject(new { query = introspectionQuery });

            return ZennoPoster.HttpPost(
                GRAPHQL_URL,
                Encoding.UTF8.GetBytes(jsonBody),
                "application/json",
                "",
                "UTF-8",
                ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.BodyOnly,
                30000,
                "",
                "HANA/v1",
                true,
                5,
                headers,
                "",
                true
            );
        }
    }
}
