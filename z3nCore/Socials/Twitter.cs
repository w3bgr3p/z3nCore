using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using z3nCore.Utilities;
using System.IO;

namespace z3nCore
{
    #region Main Class
    
    public class Twitter
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        
        // Подклассы
        public TwitterAPI API { get; private set; }
        public TwitterUI UI { get; private set; }
        public TwitterAuth Auth { get; private set; }
        public TwitterContent Content { get; private set; }
        
        /// <summary>
        /// Конструктор с Instance (полный функционал: API + UI)
        /// </summary>
        public Twitter(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), "Instance cannot be null for this constructor");
                
            _project = project;
            _instance = instance;
            _log = new Logger(project, log: log, classEmoji: "X");
            
            InitializeSubclasses();
        }
        
        /// <summary>
        /// Конструктор без Instance (только API методы)
        /// </summary>
        public Twitter(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _instance = null;
            _log = new Logger(project, log: log, classEmoji: "X");
            
            InitializeSubclasses();
        }
        
        private void InitializeSubclasses()
        {
            API = new TwitterAPI(_project, _instance, _log);
            
            if (_instance != null)
            {
                UI = new TwitterUI(_project, _instance, _log);
                Auth = new TwitterAuth(_project, _instance, _log, API);
                Content = new TwitterContent(_project, _instance, _log);
            }
        }
        
        #region Import / Parse Credentials
    
        /// <summary>
        /// Парсинг учётных данных из строки с опциональной записью в БД
        /// </summary>
        public Dictionary<string, string> ParseNewCredentials(string data, bool toDb = true)
        {
            var creds = ParseCredentials(data);
            if (toDb) _project.DicToDb(creds, "_twitter");
            return creds;
        }

        /// <summary>
        /// Статический парсинг учётных данных из строки
        /// Формат: login:password:token:ct0:email:otpsecret:emailpassword (порядок не важен, разделитель : или ;)
        /// </summary>
        public static Dictionary<string, string> ParseCredentials(string data)
        {
            var separator = data.Contains(":") ? ':' : ';';
            var creds = new Dictionary<string, string>();
            var dataparts = data.Split(separator);
            var list = dataparts.ToList();

            foreach (var part in dataparts)
            {
                if (part.Contains("@"))
                {
                    creds.Add("email", part);
                    list.Remove(part);
                }

                if (part.Length == 160)
                {
                    creds.Add("ct0", part);
                    list.Remove(part);
                }

                if (part.Length == 40)
                {
                    creds.Add("token", part);
                    list.Remove(part);
                }
            }

            creds.Add("login", list[0]);
            list.RemoveAt(0);

            creds.Add("password", list[0]);
            list.RemoveAt(0);

            foreach (var part in list.ToList())
            {
                if (Regex.IsMatch(part, "^[A-Z2-7]{16}$"))
                {
                    creds.Add("otpsecret", part);
                    list.Remove(part);
                    break;
                }
            }

            if (list.Count > 0)
            {
                creds.Add("emailpassword", list[0]);
            }
        
            return creds;
        }
    
        #endregion

        /// <summary>
    /// Batch операции над множеством аккаунтов
    /// </summary>
        public static class Batch
        {
            /// <summary>
            /// Проверка всех аккаунтов на suspended
            /// </summary>
            public static void CheckSuspended(IZennoPosterProjectModel project, string tableName = "_twitter", bool removeSuspended = false, int startRange = 0)
            {
                // 1. Найти валидный аккаунт
                Twitter validTwitter = null;
                if  (startRange == 0) startRange = project.Int("rangeStart");
                int endRange = project.Int("rangeEnd");
                
                for (int i = startRange; i <= endRange; i++)
                {
                    project.Var("acc0", i);
                    
                    if (string.IsNullOrEmpty(project.DbGet("cookies", "_instance")))
                        continue;
                    
                    try
                    {
                        var tw = new Twitter(project, log: false);
                        if (tw.API.ValidateCookiesOwnership() == "valid")
                        {
                            validTwitter = tw;
                            project.log($"☺ Using account [{project.DbGet("login", tableName)}] for batch check");
                            break;
                        }
                    }
                    catch { }
                }
                
                if (validTwitter == null)
                {
                    project.warn("!!! No valid accounts found for batch operations");
                    return;
                }
                
                // 2. Проверить все аккаунты
                project.log("=== Checking accounts for suspended status ===");
                startRange = project.Int("rangeStart");
                for (int i = startRange; i <= endRange; i++)
                {
                    Thread.Sleep(500);
                    
                    var login = project.DbGet("login", tableName, where: $"id = {i}");
                    
                    if (string.IsNullOrEmpty(login))
                        continue;
                    
                    try
                    {
                        var data = validTwitter.API.GetUserInfoByGraph(login);
                        
                        if (data.Contains("User is suspended"))
                        {
                            project.DbUpd("status = 'suspended'", tableName, where: $"id = {i}");
                            project.warn($"☠ [{i}]: {login} SUSPENDED");
                            if (removeSuspended) project.DbClearLine(i,tableName);
                        }
                        else
                        {
                            project.log($"✌ [{i}]: {login} Alive");
                        }
                    }
                    catch (Exception ex)
                    {
                        project.warn($"[{login}] Error: {ex.Message}");
                    }
                }
            }
            
            /// <summary>
            /// Валидация cookies для всех аккаунтов
            /// </summary>
            public static void ValidateAllCookies(IZennoPosterProjectModel project, string tableName = "_twitter")
            {
                int startRange = project.Int("rangeStart");
                int endRange = project.Int("rangeEnd");
                
                project.log("=== Validating cookies ownership ===");
                
                for (int i = startRange; i <= endRange; i++)
                {
                    project.Var("acc0", i);
                    var login = project.DbGet("login", tableName);
                    var status = project.DbGet("status", tableName);

                    if (string.IsNullOrEmpty(login))
                    {
                        project.CleanDomainInDb("x.com");
                        continue;
                    }
                    
                    if (status == "suspended")
                    {
                        project.log($"[{login}] Suspended, skip");
                        continue;
                    }
                    
                    if (string.IsNullOrEmpty(project.DbGet("cookies", "_instance")))
                    {
                        project.log($"[{login}] No cookies, skip");
                        continue;
                    }
                    
                    Thread.Sleep(1000);
                    
                    try
                    {
                        var tw = new Twitter(project, log: true);
                        string result = tw.API.ValidateCookiesOwnership();
                        
                        switch (result)
                        {
                            case "valid":
                                project.log($"☺ [{login}] Valid");
                                var data = tw.API.GetUserInfo(null, TwitterAPI.ToGet.Info);
                                project.DicToDb(data, tableName);
                                break;
                                
                            case "wrong_account":
                                project.warn($"✖ [{login}] WRONG ACCOUNT - clearing cookies only");
                                project.CleanDomainInDb("x.com");
                                project.DbUpd("status = 'WrongCookies'", tableName);
                                break;
                                
                            case "suspended":
                                project.warn($"✖ [{login}] Suspended in cookies = WRONG");
                                project.CleanDomainInDb("x.com");
                                project.DbUpd("status = 'WrongCookies'", tableName);
                                break;
                                
                            case "invalid":
                                project.warn($"✖ [{login}] Invalid/expired cookies");
                                project.CleanDomainInDb("x.com");
                                project.DbUpd("status = 'InvalidCookies'", tableName);
                                break;
                                
                            case "error":
                                project.warn($"✖ [{login}] Validation error");
                                project.DbUpd("status = 'ValidationError'", tableName);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        project.warn($"[{login}] Exception: {ex.Message}");
                    }
                }
            }
                        
        }
    }
    
    #endregion
    
    #region API Subclass
    
    /// <summary>
    /// GraphQL API методы (работают без браузера)
    /// </summary>
    public class TwitterAPI
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        private readonly Sleeper _idle;
        
        private string _token;
        private string _ct0;
        private string _login;
        
        internal TwitterAPI(IZennoPosterProjectModel project, Instance instance, Logger log)
        {
            _project = project;
            _instance = instance;
            _log = log;
            _idle = new Sleeper(1337, 2078);
            LoadCreds();
        }
        
        private void LoadCreds()
        {
            var creds = _project.DbGetColumns("token, ct0, login, password", "_twitter");
            _token = creds["token"];
            _ct0 = creds.ContainsKey("ct0") ? creds["ct0"] : "";
            _login = creds["login"];
        }
        
        private string[] BuildHeaders()
        {
            return new[]
            {
                $"User-Agent: {_project.Profile.UserAgent}",
                "Accept-Language: en-US,en;q=0.7",
                "authorization: Bearer AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA",
                "content-type: application/json",
                "sec-ch-ua: \"Chromium\";v=\"112\", \"Google Chrome\";v=\"112\", \";Not A Brand\";v=\"99\"",
                "sec-ch-ua-mobile: ?0",
                "sec-ch-ua-platform: \"Windows\"",
                $"x-csrf-token: {_ct0}",
                "x-twitter-active-user: yes",
                "x-twitter-auth-type: OAuth2Session",
                "x-twitter-client-language: en",
                $"Referer: https://twitter.com/{_login}",
                "Connection: keep-alive"
            };
        }
        
        private string MakeCookieFromDb()
        {
            var c = _project.DbGet("cookies", "_instance");
            var cookJson = c.FromBase64();
            cookJson = Cookies.ConvertCookieFormat(cookJson, "json");
            JArray toParse = JArray.Parse(cookJson);
            
            string guest_id = "";
            string kdt = "";
            string twid = "";
            
            for (int i = 0; i < toParse.Count; i++)
            {
                string cookieName = toParse[i]["name"].ToString();

                if (cookieName == "auth_token")
                    _token = toParse[i]["value"].ToString();
                if (cookieName == "ct0")
                    _ct0 = toParse[i]["value"].ToString();
                if (cookieName == "guest_id")
                    guest_id = toParse[i]["value"].ToString();
                if (cookieName == "kdt")
                    kdt = toParse[i]["value"].ToString();
                if (cookieName == "twid")
                    twid = toParse[i]["value"].ToString();

                if (!string.IsNullOrEmpty(_token) && !string.IsNullOrEmpty(_ct0))
                    break;
            }

            return $"guest_id={guest_id}; kdt={kdt}; auth_token={_token}; guest_id_ads={guest_id}; guest_id_marketing={guest_id}; lang=en; twid={twid}; ct0={_ct0};";
        }
        
        /// <summary>
        /// Валидация cookies через API
        /// </summary>
        public bool ValidateCookies()
        {
            _idle.Sleep();
            string cookies = MakeCookieFromDb();
            string[] headers = BuildHeaders();
    
            var url = TwitterGraphQLBuilder.BuildUserByScreenNameUrl(_login);
            var resp = _project.GET(url, "+", headers, cookies, returnSuccessWithStatus: true);
    
            // Теперь при успехе resp начинается с "200"
            return resp.StartsWith("200");
        }
        
        /// <summary>
        /// Получение информации о пользователе
        /// </summary>
        /// <param name="targetUsername">Username для поиска</param>
        /// <param name="fieldsToKeep">Какие поля вернуть (null = ToGet.Scraping по умолчанию)</param>
        public string GetUserInfoByGraph(string targetUsername = null)
        {
            if (string.IsNullOrEmpty(targetUsername)) targetUsername = _login;
            _idle.Sleep();
            string cookies = MakeCookieFromDb();
            string[] headers = BuildHeaders();
    
            var url = TwitterGraphQLBuilder.BuildUserByScreenNameUrl(targetUsername);
            return _project.GET(url, "+", headers, cookies);
        }
        public Dictionary<string, string> GetUserInfo(string targetUsername = null, string[] fieldsToKeep = null)
        {
            var resp = GetUserInfoByGraph(targetUsername);
            var fullResult = new Dictionary<string, string>();
            try
            {
                fullResult = resp.JsonToDic();
            }
            catch 
            {
                _project.warn(resp, true);
            }

            if (fieldsToKeep == null)
                return fullResult;
    
            var filtered = new Dictionary<string, string>();
            foreach (var kvp in fullResult)
            {
                if (fieldsToKeep.Contains(kvp.Key))
                    filtered[kvp.Key] = kvp.Value;
            }
            
            return BeautifyDic(filtered);
            
        }
        
        /// <summary>
        /// Пресеты полей для GetUserInfo
        /// </summary>
        public static class ToGet
        {
            public static readonly string[] Info = { 
                "data_user_result_id",
                "data_user_result_rest_id",
                "data_user_result_legacy_screen_name",
                "data_user_result_legacy_description",
                "data_user_result_legacy_location",
                "data_user_result_legacy_profile_banner_url",
                "data_user_result_legacy_profile_image_url_https",
                "data_user_result_legacy_followers_count",
                "data_user_result_legacy_friends_count",
                "data_user_result_legacy_statuses_count",
                "data_user_result_legacy_favourites_count",
                "data_user_result_legacy_listed_count",
                "data_user_result_legacy_media_count",
                "data_user_result_legacy_normal_followers_count",
                "data_user_result_legacy_fast_followers_count",
                "data_user_result_legacy_possibly_sensitive",
                "data_user_result_legacy_needs_phone_verification"
            };

            // Базовая идентификация
            public static readonly string[] Identity = { 
                "data_user_result_rest_id",
                "data_user_result_legacy_screen_name",
                "data_user_result_legacy_name"
            };
            
            // Все счётчики
            public static readonly string[] Stats = { 
                "data_user_result_legacy_followers_count",
                "data_user_result_legacy_friends_count",
                "data_user_result_legacy_statuses_count",
                "data_user_result_legacy_favourites_count",
                "data_user_result_legacy_listed_count",
                "data_user_result_legacy_media_count",
                "data_user_result_legacy_normal_followers_count",
                "data_user_result_legacy_fast_followers_count"
            };
            
            // Профиль
            public static readonly string[] Profile = { 
                "data_user_result_legacy_screen_name",
                "data_user_result_legacy_name",
                "data_user_result_legacy_description",
                "data_user_result_legacy_location",
                "data_user_result_legacy_profile_banner_url",
                "data_user_result_legacy_profile_image_url_https",
                "data_user_result_legacy_created_at",
                "data_user_result_legacy_default_profile",
                "data_user_result_legacy_default_profile_image"
            };
            
            // Статусы и верификация
            public static readonly string[] Status = { 
                "data_user_result_legacy_verified",
                "data_user_result_is_blue_verified",
                "data_user_result_legacy_possibly_sensitive",
                "data_user_result_legacy_needs_phone_verification"
            };
            
            // Разрешения
            public static readonly string[] Permissions = { 
                "data_user_result_legacy_can_dm",
                "data_user_result_legacy_can_media_tag",
                "data_user_result_smart_blocked_by",
                "data_user_result_smart_blocking"
            };
            
            // Для взаимодействия
            public static readonly string[] Engagement = { 
                "data_user_result_legacy_screen_name",
                "data_user_result_rest_id",
                "data_user_result_legacy_name",
                "data_user_result_legacy_followers_count",
                "data_user_result_legacy_friends_count",
                "data_user_result_legacy_can_dm",
                "data_user_result_legacy_verified",
                "data_user_result_legacy_description"
            };
            
            // Для парсинга
            public static readonly string[] Scraping = { 
                "data_user_result_legacy_screen_name",
                "data_user_result_rest_id",
                "data_user_result_legacy_name",
                "data_user_result_legacy_description",
                "data_user_result_legacy_location",
                "data_user_result_legacy_profile_banner_url",
                "data_user_result_legacy_profile_image_url_https",
                "data_user_result_legacy_followers_count",
                "data_user_result_legacy_friends_count",
                "data_user_result_legacy_statuses_count",
                "data_user_result_legacy_created_at",
                "data_user_result_legacy_verified",
                "data_user_result_is_blue_verified"
            };
            
            // День рождения
            public static readonly string[] Birthdate = { 
                "data_user_result_legacy_extended_profile_birthdate_day",
                "data_user_result_legacy_extended_profile_birthdate_month",
                "data_user_result_legacy_extended_profile_birthdate_year",
                "data_user_result_legacy_extended_profile_birthdate_visibility",
                "data_user_result_legacy_extended_profile_birthdate_year_visibility"
            };
            
            // Монетизация
            public static readonly string[] Monetization = { 
                "data_user_result_creator_subscriptions_count",
                "data_user_result_has_graduated_access"
            };
            
            // ВСЕ остальные технические поля
            public static readonly string[] All = {
                "data_user_result___typename",
                "data_user_result_id",
                "data_user_result_rest_id",
                "data_user_result_has_graduated_access",
                "data_user_result_is_blue_verified",
                "data_user_result_profile_image_shape",
                "data_user_result_legacy_can_dm",
                "data_user_result_legacy_can_media_tag",
                "data_user_result_legacy_created_at",
                "data_user_result_legacy_default_profile",
                "data_user_result_legacy_default_profile_image",
                "data_user_result_legacy_description",
                "data_user_result_legacy_fast_followers_count",
                "data_user_result_legacy_favourites_count",
                "data_user_result_legacy_followers_count",
                "data_user_result_legacy_friends_count",
                "data_user_result_legacy_has_custom_timelines",
                "data_user_result_legacy_is_translator",
                "data_user_result_legacy_listed_count",
                "data_user_result_legacy_location",
                "data_user_result_legacy_media_count",
                "data_user_result_legacy_name",
                "data_user_result_legacy_needs_phone_verification",
                "data_user_result_legacy_normal_followers_count",
                "data_user_result_legacy_possibly_sensitive",
                "data_user_result_legacy_profile_banner_url",
                "data_user_result_legacy_profile_image_url_https",
                "data_user_result_legacy_screen_name",
                "data_user_result_legacy_statuses_count",
                "data_user_result_legacy_translator_type",
                "data_user_result_legacy_verified",
                "data_user_result_legacy_want_retweets",
                "data_user_result_smart_blocked_by",
                "data_user_result_smart_blocking",
                "data_user_result_legacy_extended_profile_birthdate_day",
                "data_user_result_legacy_extended_profile_birthdate_month",
                "data_user_result_legacy_extended_profile_birthdate_year",
                "data_user_result_legacy_extended_profile_birthdate_visibility",
                "data_user_result_legacy_extended_profile_birthdate_year_visibility",
                "data_user_result_is_profile_translatable",
                "data_user_result_highlights_info_can_highlight_tweets",
                "data_user_result_highlights_info_highlighted_tweets",
                "data_user_result_creator_subscriptions_count"
            };
        }
        private static Dictionary<string, string> BeautifyDic(Dictionary<string, string> dic)
        {
            var beauty = new Dictionary<string, string>();
            
            foreach( var p in dic)
            {
                if (p.Key.Contains("data_user_result_id"))
                {
                    beauty.Add("_id", p.Value);
                }
                else if (p.Key.Contains("data_user_result_legacy_extended_profile_"))
                {
                    beauty.Add(p.Key.Replace("data_user_result_legacy_extended_profile_",""), p.Value);
                }
                else if (p.Key.Contains("data_user_result_smart_"))
                {
                    beauty.Add(p.Key.Replace("data_user_result_smart_",""), p.Value);
                }
                else if (p.Key.Contains("data_user_result_legacy_"))
                {
                    beauty.Add(p.Key.Replace("data_user_result_legacy_",""), p.Value);
                }
                else if (p.Key.Contains("data_user_result_"))
                {
                    beauty.Add(p.Key.Replace("data_user_result_",""), p.Value);
                }
                else 
                    beauty.Add(p.Key, p.Value);
                
            }
            return beauty;
            
        }
        
       /// <summary>
        /// Проверка что cookies принадлежат нужному аккаунту
        /// Возвращает: "valid" | "wrong_account" | "invalid" | "error"
        /// </summary>
        public string ValidateCookiesOwnership()
        {
            try
            {
                _idle.Sleep();
                string cookies = MakeCookieFromDb();
                string[] headers = BuildHeaders();
                
                var url = TwitterGraphQLBuilder.BuildUserByScreenNameUrl(_login);
                var resp = _project.GET(url, "+", headers, cookies, returnSuccessWithStatus: true);
                
                // Теперь ВСЕГДА начинается с кода
                if (resp.StartsWith("401") || resp.StartsWith("403"))
                {
                    _log.Warn(resp);
                    return "invalid";
                }

                if (!resp.StartsWith("200"))
                {
                    _log.Warn($"HTTP error: {resp.Substring(0, Math.Min(100, resp.Length))}");
                    return "error";
                }
                
                // Извлекаем body после "\r\n\r\n"
                int bodyStart = resp.IndexOf("\r\n\r\n");
                if (bodyStart == -1)
                {
                    _log.Warn("Cannot parse response format");
                    return "error";
                }
                
                string body = resp.Substring(bodyStart + 4);
                
                // Проверка на suspended
                if (body.Contains("UserUnavailable") && body.Contains("Suspended"))
                {
                    _log.Warn("Account in cookies suspended");
                    return "suspended";
                }
                
                var userInfo = body.JsonToDic();
                
                string currentScreenName = "";
                if (userInfo.ContainsKey("data_user_result_legacy_screen_name"))
                    currentScreenName = userInfo["data_user_result_legacy_screen_name"];
                
                if (string.IsNullOrEmpty(currentScreenName))
                {
                    _log.Warn($"Cannot extract screen_name from [{body}]");
                    return "error";
                }
                
                if (currentScreenName.ToLower() != _login.ToLower())
                {
                    _log.Warn($"WRONG ACCOUNT: [{currentScreenName}] != [{_login}]", show: true);
                    return "wrong_account";
                }
                
                _log.Send($"☺ Valid: [{currentScreenName}]");
                return "valid";
            }
            catch (Exception ex)
            {
                _log.Warn($"Exception: {ex.Message}");
                return "error";
            }
        }        
       /// <summary>
        /// Проверка аккаунта на suspended через API с использованием ЧУЖОГО токена
        /// </summary>
        /// <param name="username">Username для проверки</param>
        /// <param name="validToken">Любой валидный токен для запроса</param>
        /// <param name="validCt0">ct0 для этого токена</param>
        public static string CheckAccountStatus(IZennoPosterProjectModel project, string username, string validToken, string validCt0)
        {
            var logger = new Logger(project, log: true, classEmoji: "X");
            var idle = new Sleeper(1337, 2078);
            
            try
            {
                idle.Sleep();
                
                // Формируем cookies с чужим токеном
                string cookies = $"auth_token={validToken}; ct0={validCt0};";
                
                string[] headers = new[]
                {
                    $"User-Agent: {project.Profile.UserAgent}",
                    "authorization: Bearer AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA",
                    "content-type: application/json",
                    $"x-csrf-token: {validCt0}",
                    "x-twitter-active-user: yes",
                    "x-twitter-auth-type: OAuth2Session",
                    "x-twitter-client-language: en"
                };
                
                var url = TwitterGraphQLBuilder.BuildUserByScreenNameUrl(username);
                var resp = project.GET(url, "+", headers, cookies, returnSuccessWithStatus: true);
                
                if (resp.StartsWith("401") || resp.StartsWith("403"))
                {
                    logger.Warn($"[{username}] Token validation failed");
                    return "error";
                }
                
                if (!resp.StartsWith("200"))
                {
                    logger.Warn($"[{username}] HTTP error: {resp.Substring(0, Math.Min(50, resp.Length))}");
                    return "error";
                }
                
                int bodyStart = resp.IndexOf("\r\n\r\n");
                string body = resp.Substring(bodyStart + 4);
                
                // Проверка на suspended
                if (body.Contains("UserUnavailable") && body.Contains("Suspended"))
                {
                    logger.Warn($"[{username}] SUSPENDED");
                    return "suspended";
                }
                
                // Проверка что аккаунт существует и доступен
                if (body.Contains("\"__typename\":\"User\""))
                {
                    logger.Send($"[{username}] OK (alive)");
                    return "ok";
                }
                
                logger.Warn($"[{username}] Unknown status");
                return "error";
            }
            catch (Exception ex)
            {
                logger.Warn($"[{username}] Exception: {ex.Message}");
                return "error";
            }
        }
       
    }
    
    #endregion
    
    #region UI Subclass
    
    /// <summary>
    /// UI методы (требуют Instance)
    /// </summary>
    public class TwitterUI
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        private readonly Sleeper _idle;
        
        private string _login;
        private string _pass;
        
        internal TwitterUI(IZennoPosterProjectModel project, Instance instance, Logger log)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _log = log;
            _idle = new Sleeper(1337, 2078);
            
            var creds = _project.DbGetColumns("login, password", "_twitter");
            _login = creds["login"];
            _pass = creds["password"];
        }
        
        /// <summary>
        /// Переход на профиль
        /// </summary>
        public void GoToProfile(string profile = null)
        {
            if (string.IsNullOrEmpty(profile))
            {
                if (!_instance.ActiveTab.URL.Contains(_login))
                    _instance.HeClick(("*", "data-testid", "AppTabBar_Profile_Link", "regexp", 0));
            }
            else
            {
                if (!_instance.ActiveTab.URL.Contains(profile))
                    _instance.ActiveTab.Navigate($"https://x.com/{profile}", "");
            }
        }
        
        /// <summary>
        /// Закрыть стандартные попапы
        /// </summary>
        public void SkipDefaultButtons()
        {
            _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 0, thr0w: false);
            _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);
            _instance.HeClick(("button", "innertext", "Got\\ it", "regexp", 0), deadline: 0, thr0w: false);
        }
        
        /// <summary>
        /// Отправить твит
        /// </summary>
        public void SendTweet(string tweet, string accountToMention = null)
        {
            GoToProfile(accountToMention);
            _instance.HeClick(("a", "data-testid", "SideNav_NewTweet_Button", "regexp", 0));
            _instance.HeClick(("div", "class", "notranslate\\ public-DraftEditor-content", "regexp", 0), delay: 2);
            _instance.CtrlV(tweet);
            _instance.HeClick(("button", "data-testid", "tweetButton", "regexp", 0), delay: 2);
            
            try
            {
                var toast = _instance.HeGet(("*", "data-testid", "toast", "regexp", 0));
                _project.log(toast);
            }
            catch (Exception ex)
            {
                _log.Warn(ex.Message, thrw: true);
            }
        }
        
        /// <summary>
        /// Отправить тред
        /// </summary>
        public void SendThread(List<string> tweets, string accountToMention = null)
        {
            GoToProfile(accountToMention);
            var title = tweets[0];
            tweets.RemoveAt(0);

            if (tweets.Count == 0)
            {
                SendTweet(title, accountToMention);
                return;
            }
            _instance.HeClick(("*", "data-testid", "SideNav_NewTweet_Button", "regexp", 0), emu:1);
            _instance.JsSet("[data-testid='tweetTextarea_0']", title);
            _idle.Sleep();

            int tIndex = 1;
            foreach (var add in tweets)
            {
                _instance.HeClick(("*", "data-testid", "addButton", "regexp", 0), emu:1);
                _idle.Sleep();
                _instance.JsSet($"[data-testid='tweetTextarea_{tIndex}']", add);
                _idle.Sleep();
                tIndex++;
            }
            _instance.HeClick(("*", "data-testid", "tweetButton", "regexp", 0), emu:1);

            try
            {
                var toast = _instance.HeGet(("*", "data-testid", "toast", "regexp", 0));
                _project.log(toast);
            }
            catch (Exception ex)
            {
                _log.Warn(ex.Message, thrw: true);
            }
        }
        
        /// <summary>
        /// Подписаться на текущий профиль
        /// </summary>
        public void Follow()
        {
            try
            {
                _instance.HeGet(("button", "data-testid", "-unfollow", "regexp", 0), deadline: 1);
                return;
            }
            catch { }
            
            var flwButton = _instance.HeGet(("button", "data-testid", "-follow", "regexp", 0));
            if (flwButton.Contains("Follow"))
            {
                try
                {
                    _instance.HeClick(("button", "data-testid", "-follow", "regexp", 0));
                }
                catch (Exception ex)
                {
                    _log.Warn(ex.Message, thrw: true);
                }
            }
        }
        
        /// <summary>
        /// Лайкнуть случайный пост с профиля
        /// </summary>
        public void RandomLike(string targetAccount = null)
        {
            _project.Deadline();
            if (targetAccount != null)
                GoToProfile(targetAccount);

            while (true)
            {
                _project.Deadline(30);
                Thread.Sleep(1000);
                var wall = _instance.ActiveTab.FindElementsByAttribute("div", "data-testid", "cellInnerDiv", "regexp").ToList();
                
                var allElements = new List<(HtmlElement Element, HtmlElement Parent, string TestId)>();
                foreach (HtmlElement tweet in wall)
                {
                    var tweetData = tweet.GetChildren(true);
                    foreach (HtmlElement he in tweetData)
                    {
                        var testId = he.GetAttribute("data-testid");
                        if (testId != "")
                            allElements.Add((he, tweet, testId));
                    }
                }

                string condition = Regex.Replace(_instance.ActiveTab.URL, "https://x.com/", "");
                var likeElements = allElements
                    .Where(x => x.TestId == "like")
                    .Where(x => allElements.Any(y => y.Parent == x.Parent && y.TestId == "User-Name" && y.Element.InnerText.Contains(condition)))
                    .Select(x => x.Element)
                    .ToList();

                if (likeElements.Count > 0)
                {
                    Random rand = new Random();
                    HtmlElement randomLike = likeElements[rand.Next(likeElements.Count)];
                    _instance.HeClick(randomLike, emu: 1);
                    break;
                }
                else
                {
                    _log.Send($"No posts from [{condition}]");
                    _instance.ScrollDown();
                }
            }
        }
        
        /// <summary>
        /// Ретвитнуть случайный пост с профиля
        /// </summary>
        public void RandomRetweet(string targetAccount = null)
        {
            _project.Deadline();
            if (targetAccount != null)
                GoToProfile(targetAccount);

            while (true)
            {
                _project.Deadline(30);
                Thread.Sleep(2000);
                var wall = _instance.ActiveTab.FindElementsByAttribute("div", "data-testid", "cellInnerDiv", "regexp").ToList();
                
                var allElements = new List<(HtmlElement Element, HtmlElement Parent, string TestId)>();
                foreach (HtmlElement tweet in wall)
                {
                    var tweetData = tweet.GetChildren(true);
                    foreach (HtmlElement he in tweetData)
                    {
                        var testId = he.GetAttribute("data-testid");
                        if (testId != "")
                            allElements.Add((he, tweet, testId));
                    }
                }
                
                string condition = Regex.Replace(_instance.ActiveTab.URL, "https://x.com/", "");
                var retweetElements = allElements
                    .Where(x => x.TestId == "retweet")
                    .Where(x => allElements.Any(y => y.Parent == x.Parent && y.TestId == "User-Name" && y.Element.InnerText.Contains(condition)))
                    .Select(x => x.Element)
                    .ToList();
                
                if (retweetElements.Count > 0)
                {
                    Random rand = new Random();
                    HtmlElement randomRetweet = retweetElements[rand.Next(retweetElements.Count)];
                    _instance.HeClick(randomRetweet, emu: 1);
                    Thread.Sleep(1000);
                    
                    HtmlElement dropdown = _instance.ActiveTab.FindElementByAttribute("div", "data-testid", "Dropdown", "regexp", 0);
                    if (!dropdown.FindChildByAttribute("div", "data-testid", "unretweetConfirm", "text", 0).IsVoid)
                    {
                        _instance.ScrollDown();
                        continue;
                    }
                    else
                    {
                        var confirm = dropdown.FindChildByAttribute("div", "data-testid", "retweetConfirm", "text", 0);
                        _instance.HeClick(confirm, emu: 1);
                        break;
                    }
                }
                else
                {
                    _log.Send($"No posts from [{condition}]");
                    _instance.ScrollDown();
                }
            }
        }
        
        /// <summary>
        /// Получить текущий email аккаунта
        /// </summary>
        public string GetCurrentEmail()
        {
            _instance.Go("https://x.com/settings/email");
            try
            {
                _instance.HeSet(("current_password", "name"), _pass, deadline: 1);
                _instance.HeClick(("button", "innertext", "Confirm", "regexp", 0));
            }
            catch { }

            string email = _instance.HeGet(("current_email", "name"), atr: "value");
            return email.ToLower();
        }
        
        /// <summary>
        /// Проверка и нажатие кнопки Retry если страница сломалась
        /// </summary>
        public void Retry()
        {
            _project.Deadline();
            Thread.Sleep(2000);
            while (true)
            {
                _project.Deadline(60);
                if (!_instance.ActiveTab.FindElementByAttribute("button", "innertext", "Retry", "regexp", 0).IsVoid)
                {
                    _log.Send("Page fucked up, clicking Retry...");
                    _instance.HeClick(("button", "innertext", "Retry", "regexp", 0), emu: 1);
                    Thread.Sleep(5000);
                    continue;
                }
                break;
            }
        }
        
        #region Intent Links
        
        public void FollowByLink(string screen_name)
        {
            Tab tab = _instance.NewTab("twitter");
            _instance.Go($"https://x.com/intent/follow?screen_name={screen_name}");
            _idle.Sleep();
            _instance.HeGet(("button", "data-testid", "confirmationSheetConfirm", "regexp", 0));
            _instance.HeClick(("*", "data-testid", "confirmationSheetConfirm", "regexp", 0), emu:1);
            _idle.Sleep();
            tab.Close();
        }

        public void QuoteByLink(string tweeturl)
        {
            Tab tab = _instance.NewTab("twitter");
            string text = Uri.EscapeDataString(tweeturl);
            _instance.Go($"https://x.com/intent/post?text={text}");
            _idle.Sleep();
            _instance.HeGet(("button", "data-testid", "confirmationSheetConfirm", "regexp", 0));
            _instance.HeClick(("*", "data-testid", "confirmationSheetConfirm", "regexp", 0), emu:1);
            _idle.Sleep();
            tab.Close();
        }

        public void RetweetByLink(string tweet_id)
        {
            Tab tab = _instance.NewTab("twitter");
            _instance.Go($"https://x.com/intent/retweet?tweet_id={tweet_id}");
            _idle.Sleep();
            _instance.HeGet(("button", "data-testid", "confirmationSheetConfirm", "regexp", 0));
            _instance.HeClick(("*", "data-testid", "confirmationSheetConfirm", "regexp", 0), emu:1);
            _idle.Sleep();
            tab.Close();
        }

        public void LikeByLink(string tweet_id)
        {
            Tab tab = _instance.NewTab("twitter");
            _instance.Go($"https://x.com/intent/like?tweet_id={tweet_id}");
            _idle.Sleep();
            _instance.HeGet(("button", "data-testid", "confirmationSheetConfirm", "regexp", 0));
            _instance.HeClick(("*", "data-testid", "confirmationSheetConfirm", "regexp", 0), emu:1);
            _idle.Sleep();
            tab.Close();
        }

        public void ReplyByLink(string tweet_id, string text)
        {
            Tab tab = _instance.NewTab("twitter");
            string escapedText = Uri.EscapeDataString(text);
            _instance.Go($"https://x.com/intent/post?in_reply_to={tweet_id}&text={escapedText}");
            _idle.Sleep();
            _instance.HeGet(("button", "data-testid", "tweetButton", "regexp", 0));
            _instance.HeClick(("*", "data-testid", "tweetButton", "regexp", 0), emu:1);
            _idle.Sleep();
            tab.Close();
        }
        
        #endregion
    }
    
    #endregion
    
    #region Auth Subclass
    
    /// <summary>
    /// Авторизация и управление токенами
    /// </summary>
    public class TwitterAuth
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        private readonly Sleeper _idle;
        private readonly TwitterAPI _api;
        
        private string _login;
        private string _pass;
        private string _2fa;
        private string _token;
        private string _ct0;
        
        internal TwitterAuth(IZennoPosterProjectModel project, Instance instance, Logger log, TwitterAPI api)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _log = log;
            _idle = new Sleeper(1337, 2078);
            _api = api;
            
            LoadCreds();
        }
        
        private void LoadCreds()
        {
            var creds = _project.DbGetColumns("login, password, otpsecret, token, ct0", "_twitter");
            _login = creds["login"];
            _pass = creds["password"];
            _2fa = creds["otpsecret"];
            _token = creds["token"];
            _ct0 = creds.ContainsKey("ct0") ? creds["ct0"] : "";
        }
        
        /// <summary>
        /// Установить токен через JavaScript
        /// </summary>
        public void SetToken(string token = null)
        {
            if (string.IsNullOrEmpty(token)) token = _token;
            string jsCode = _project.ExecuteMacro($"document.cookie = \"auth_token={token}; domain=.x.com; path=/; expires=${DateTimeOffset.UtcNow.AddYears(1).ToString("R")}; Secure\";\r\nwindow.location.replace(\"https://x.com\")");
            _instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
            _log.Send($"Token applied: {token.Substring(0, 10)}...");
            _instance.F5();
            Thread.Sleep(3000);
        }
        
        /// <summary>
        /// Получить токены из браузера
        /// </summary>
        public void ExtractTokens()
        {
            var cookJson = _instance.GetCookies(".");//new Cookies(_project, _instance).Get(".");
            JArray toParse = JArray.Parse(cookJson);
            
            string token = "";
            string ct0 = "";
            
            for (int i = 0; i < toParse.Count; i++)
            {
                string cookieName = toParse[i]["name"].ToString();
                if (cookieName == "auth_token")
                    token = toParse[i]["value"].ToString();
                if (cookieName == "ct0")
                    ct0 = toParse[i]["value"].ToString();
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(ct0))
                    break;
            }

            _token = token;
            _ct0 = ct0;
            _project.DbUpd($"token = '{token}', ct0 = '{ct0}'", "_twitter");
            _log.Send($"Tokens extracted: auth_token={token.Length} chars, ct0={ct0.Length} chars");
        }
        
        /// <summary>
        /// Логин с учётными данными
        /// </summary>
        public string LoginWithCredentials()
        {
            if (_instance.ActiveTab.FindElementByAttribute("input:text", "autocomplete", "username", "text", 0).IsVoid)
            {
                _instance.ActiveTab.Navigate("https://x.com/", "");
                _idle.Sleep();
                _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 1, thr0w: false);
                _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);
                _instance.HeClick(("a", "data-testid", "login", "regexp", 0));
            }

            _instance.JsSet("[autocomplete='username']", _login);
            _idle.Sleep();
            _instance.SendText("{ENTER}", 15);
            
            var toast = CatchToast();
            if (toast.Contains("Could not log you in now."))
                return toast;

            var err = CatchErr();
            if (err != "") return err;
            
            _instance.JsSet("[name='password']", _pass);
            _idle.Sleep();
            _instance.SendText("{ENTER}", 15);
            _idle.Sleep();
            
            err = CatchErr();
            if (err != "") return err;
            
            var codeOTP = OTP.Offline(_2fa);
            _instance.JsSet("[name='text']", codeOTP);
            _idle.Sleep();
            _instance.SendText("{ENTER}", 15);
            _idle.Sleep();
            
            err = CatchErr();
            if (err != "") return err;

            _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 1, thr0w: false);
            _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);
            ExtractTokens();
            return "ok";
        }
        
        private string CatchErr()
        {
            if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Sorry, we could not find your account')]", 0).IsVoid) 
                return "NotFound";
            if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Wrong password!')]", 0).IsVoid)
                return "WrongPass";
            if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Your account is suspended')]", 0).IsVoid)
                return "Suspended";
            if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Oops,\\ something\\ went\\ wrong.\\ Please\\ try\\ again\\ later.", "regexp", 0).IsVoid) 
                return "SomethingWentWrong";
            if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", "Suspicious\\ login\\ prevented", "regexp", 0).IsVoid) 
                return "SuspiciousLogin";
            return "";
        }

        private string CatchToast()
        {
            var err = "";
            try
            {
                err = _instance.HeGet(("div", "data-testid", "toast", "regexp", 0), deadline: 2);
            }
            catch { }
            
            if (err != "" && err.Contains("Could not log you in now."))
            {
                _project.warn(err);
                return err;
            }
            return "";
        }
        
        /// <summary>
        /// Полный процесс логина с фоллбэками
        /// </summary>
        public string Load()
        {
            bool tokenUsed = false;
            DateTime deadline = DateTime.Now.AddSeconds(60);
            
            check:
            if (DateTime.Now > deadline) throw new Exception("timeout");

            var status = CheckLoginState();
            _project.Var("status", status);

            if (status == "login" && !tokenUsed)
            {
                if (!string.IsNullOrEmpty(_token) && !string.IsNullOrEmpty(_ct0))
                {
                    try
                    {
                        bool isTokenValid = _api.ValidateCookies();
                        _log.Send($"Token API Check: {(isTokenValid ? "Valid" : "Invalid")}");
                        
                        if (isTokenValid)
                        {
                            SetToken();
                            tokenUsed = true;
                            Thread.Sleep(5000);
                        }
                        else
                        {
                            tokenUsed = true;
                            status = LoginWithCredentials();
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Token validation error: {ex.Message}");
                        tokenUsed = true;
                        status = LoginWithCredentials();
                    }
                }
                else
                {
                    tokenUsed = true;
                    status = LoginWithCredentials();
                }
            }
            else if (status == "login" && tokenUsed)
            {
                status = LoginWithCredentials();
                Thread.Sleep(3000);
            }
            else if (status == "mixed")
            {
                _instance.CloseAllTabs();
                _instance.ClearCookie("x.com");
                _instance.ClearCache("x.com");
                _instance.ClearCookie("twitter.com");
                _instance.ClearCache("twitter.com");
                goto check;
            }
            
            _project.DbUpd($"status = '{status}'", "_twitter");
            
            if (status == "restricted" || status == "suspended" || status == "emailCapcha" || 
                status == "WrongPass" || status == "Suspended" || status == "NotFound" || 
                status.Contains("Could not log you in now."))
            {
                return status;
            }
            else if (status == "ok")
            {
                _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 0, thr0w: false);
                _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);
                ExtractTokens();
                return status;
            }

            goto check;
        }
        
        private string CheckLoginState()
        {
            DateTime start = DateTime.Now;
            DateTime deadline = DateTime.Now.AddSeconds(60);
            _instance.ActiveTab.Navigate($"https://x.com/{_login}", "");
            var status = "";

            while (string.IsNullOrEmpty(status))
            {
                Thread.Sleep(5000);
                if (DateTime.Now > deadline) throw new Exception("timeout");

                if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", @"Caution:\s+This\s+account\s+is\s+temporarily\s+restricted", "regexp", 0).IsVoid)
                    status = "restricted";
                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", @"Account\s+suspended", "regexp", 0).IsVoid)
                    status = "suspended";
                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", @"Log\ in", "regexp", 0).IsVoid)
                    status = "login";
                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", "erify\\ your\\ email", "regexp", 0).IsVoid)
                    status = "emailCapcha";
                else if (!_instance.ActiveTab.FindElementByAttribute("button", "data-testid", "SideNav_AccountSwitcher_Button", "regexp", 0).IsVoid)
                {
                    var check = _instance.ActiveTab.FindElementByAttribute("button", "data-testid", "SideNav_AccountSwitcher_Button", "regexp", 0)
                        .FirstChild.FirstChild.GetAttribute("data-testid");
                    status = (check == $"UserAvatar-Container-{_login}") ? "ok" : "mixed";
                }
                else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Something\\ went\\ wrong", "regexp", 0).IsVoid)
                {
                    _instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");
                    Thread.Sleep(3000);
                    continue;
                }
            }

            return status;
        }
    }
    
    #endregion
    
    #region Content Subclass
    

    /// <summary>
    /// Генерация контента через AI
    /// </summary>
    public class TwitterContent
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        
        private string _login;
        
        internal TwitterContent(IZennoPosterProjectModel project, Instance instance, Logger log)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _log = log;
            
            _login = _project.DbGet("login", "_twitter");
        }
        
        /// <summary>
        /// Генерация контента на основе новостной статьи через AI
        /// </summary>
        /// <param name="purpose">tweet, thread, или opinionThread</param>
        public string Generate_(string purpose = "tweet", string model = "meta-llama/Llama-3.3-70B-Instruct")
        {
            var randomNews = Rnd.RndFile(Path.Combine(_project.Path, ".data", "news"), "json");
            _project.ToJson(File.ReadAllText(randomNews));
            var article = _project.Json.FullText;
            var ai = new Api.AI(_project, "aiio", model:model, false);
            var bio = _project.DbGet("bio", "_profile");
            
            string system = "";
            if (purpose == "tweet")
                system = $@"You are an individual with your own perspective. Your personality and thinking style: {bio}

    Your task: Write ONE authentic tweet about the article below.

    CRITICAL REQUIREMENTS:
    - Maximum 280 characters total
    - Write as YOU would naturally explain this to a friend over coffee
    - Pick ONE specific detail that caught your attention (a number, name, statistic, or concept)
    - Explain what makes it interesting or why it matters, using your authentic voice
    - NO corporate jargon: avoid 'revolutionizing', 'game-changing', 'unlocking potential', 'the future of'
    - NO generic phrases: avoid 'interesting article', 'great read', 'worth checking out'
    - Sound like a human sharing genuine insight, not a press release or AI summary
    - Vary your sentence structure - don't start every tweet the same way
    - Mix direct statements with reactions naturally

    Return ONLY a clean JSON object:
    {{
      ""statement"": ""your tweet text here""
    }}

    No markdown formatting, no extra text, just the JSON.";

            else if (purpose == "thread") 
                system = $@"You are an individual with your own perspective. Your personality and thinking style: {bio}

    Your task: Write a tweet thread summarizing the article below.

    CRITICAL REQUIREMENTS:
    - Each tweet maximum 280 characters
    - First tweet: Hook readers with the most compelling point (not a generic intro)
    - Middle tweets: Break down key insights with specific details (numbers, names, facts)
    - Last tweet: Your personal takeaway or what this means practically
    - Write conversationally - like explaining to a friend, not presenting to a board
    - Include SPECIFIC examples from the article (exact figures, names, technical terms)
    - NO marketing language: avoid 'revolutionizing', 'game-changing', 'unlocking', 'disrupting'
    - NO generic transitions: avoid 'moreover', 'furthermore', 'in conclusion'
    - Vary your phrasing naturally - don't repeat sentence patterns
    - Sound authentic - mix observations with reactions, questions with statements

    Return ONLY a clean JSON object:
    {{
      ""summary_statements"": [""tweet 1"", ""tweet 2"", ""tweet 3""],
      ""opinion_theses"": [""thesis 1"", ""thesis 2""]
    }}

    No markdown formatting, no extra text, just the JSON.";

            else if (purpose == "opinionThread") 
                system = $@"You are an individual with your own perspective. Your personality and thinking style: {bio}

    Your task: Write a 2-tweet thread about the article below.

    CRITICAL REQUIREMENTS:
    - Each tweet maximum 280 characters
    - Tweet 1: Summarize the core point with ONE specific detail (number, name, or concept)
    - Tweet 2: Your authentic reaction or what this means to you
    - Write naturally - like texting a friend about something you just read
    - Pick ONE angle that resonates with your perspective from the bio
    - NO marketing speak: avoid 'revolutionizing', 'game-changing', 'transformative'
    - NO formulaic phrases: vary how you express thoughts and reactions
    - Include specific details that make it concrete and believable
    - Sound human - mix different sentence structures and rhythms

    Return ONLY a clean JSON object:
    {{
      ""summary_statement"": ""first tweet with summary"",
      ""opinion_statement"": ""second tweet with your take""
    }}

    No markdown formatting, no extra text, just the JSON.";
            else
                _log.Warn($"UNKNOWN PURPOSE: {purpose}");
            
            string result = ai.Query(system, article).Replace("```json", "").Replace("```", "");
            _project.ToJson(result);
            _log.Send($"Generated {purpose}: length={result.Length}");
            return result;
        }
        /// <summary>
        /// Генерация контента на основе новостной статьи через AI
        /// </summary>
        /// <param name="purpose">tweet, thread, или opinionThread</param>
        public string Generate(string purpose = "tweet", string model = "meta-llama/Llama-3.3-70B-Instruct")
        {
            var randomNews = Rnd.RndFile(Path.Combine(_project.Path, ".data", "news"), "json");
            _project.ToJson(File.ReadAllText(randomNews));
            var article = _project.Json.FullText;
            var ai = new Api.AI(_project, "aiio", model:model, false);
            var bio = _project.DbGet("bio", "_profile");
            
            // Рандомизация подхода к контенту
            var approaches = new[] {
                "Pick ONE number or statistic from the article and explain what makes it significant",
                "Focus on a specific person or company mentioned and what they're doing differently",
                "Find an unexpected consequence or side effect discussed in the article",
                "Identify what's changing from the old way to the new way",
                "Spot a contrarian take or counterintuitive point made in the article",
                "Highlight a specific technical detail and why it matters practically",
                "Find the underlying trend or pattern this example represents"
            };
            var randomApproach = approaches[new Random().Next(approaches.Length)];
            
            string system = "";
            if (purpose == "tweet")
                system = $@"You are an individual with your own perspective. Your personality and thinking style: {bio}

                            Your task: Write ONE authentic tweet about the article below.

                            Content approach: {randomApproach}

                            CRITICAL REQUIREMENTS:
                            - Maximum 280 characters total
                            - Write as you would naturally explain this to a friend over coffee
                            - NO corporate jargon: avoid 'revolutionizing', 'game-changing', 'unlocking potential', 'the future of'
                            - NO generic phrases: avoid 'interesting article', 'great read', 'worth checking out'
                            - Sound like a human sharing genuine insight, not a press release or AI summary
                            - Use varied sentence structures naturally - mix short and long, statements and observations

                            Return ONLY a clean JSON object:
                            {{
                              ""statement"": ""your tweet text here""
                            }}

                            No markdown formatting, no extra text, just the JSON.";

            else if (purpose == "thread") 
                system = $@"You are an individual with your own perspective. Your personality and thinking style: {bio}

                            Your task: Write a tweet thread summarizing the article below.

                            Content approach for the main point: {randomApproach}

                            CRITICAL REQUIREMENTS:
                            - Each tweet maximum 280 characters
                            - First tweet: Hook readers with the most compelling point (not a generic intro)
                            - Middle tweets: Break down key insights with specific details (numbers, names, facts)
                            - Last tweet: Your personal takeaway or what this means practically
                            - Write conversationally - like explaining to a friend, not presenting to a board
                            - Include SPECIFIC examples from the article (exact figures, names, technical terms)
                            - NO marketing language: avoid 'revolutionizing', 'game-changing', 'unlocking', 'disrupting'
                            - NO generic transitions: avoid 'moreover', 'furthermore', 'in conclusion'
                            - Sound authentic - mix observations with reactions naturally

                            Return ONLY a clean JSON object:
                            {{
                              ""summary_statements"": [""tweet 1"", ""tweet 2"", ""tweet 3""],
                              ""opinion_theses"": [""thesis 1"", ""thesis 2""]
                            }}

                            No markdown formatting, no extra text, just the JSON.";

            else if (purpose == "opinionThread") 
                system = $@"You are an individual with your own perspective. Your personality and thinking style: {bio}

                            Your task: Write a 2-tweet thread about the article below.

                            Content approach: {randomApproach}

                            CRITICAL REQUIREMENTS:
                            - Each tweet maximum 280 characters
                            - Tweet 1: Summarize the core point with ONE specific detail (number, name, or concept)
                            - Tweet 2: Your authentic reaction or what this means to you
                            - Write naturally - like texting a friend about something you just read
                            - NO marketing speak: avoid 'revolutionizing', 'game-changing', 'transformative'
                            - Include specific details that make it concrete and believable
                            - Use natural varied phrasing

                            Return ONLY a clean JSON object:
                            {{
                              ""summary_statement"": ""first tweet with summary"",
                              ""opinion_statement"": ""second tweet with your take""
                            }}

                            No markdown formatting, no extra text, just the JSON.";
            else
                _log.Warn($"UNKNOWN PURPOSE: {purpose}");
            
            string result = ai.Query(system, article).Replace("```json", "").Replace("```", "");
            _project.ToJson(result);
            _log.Send($"Generated {purpose} with approach: {randomApproach}");
            return result;
        }
        /// <summary>
        /// Сгенерировать и опубликовать твит
        /// </summary>
        public void PostGeneratedTweet()
        {
            if (!_instance.ActiveTab.URL.Contains(_login))
                _instance.HeClick(("*", "data-testid", "AppTabBar_Profile_Link", "regexp", 0));

            int tries = 5;
            gen:
            tries--;
            if (tries == 0) throw new Exception("generation problem");

            Generate("tweet");
            string tweet = _project.Json.statement;

            if (tweet.Length > 280)
            {
                _log.Warn($"Regenerating (tries: {tries}) (Exceed 280char): {tweet}");
                goto gen;
            }
            
            _instance.HeClick(("a", "data-testid", "SideNav_NewTweet_Button", "regexp", 0));
            _instance.HeClick(("div", "class", "notranslate\\ public-DraftEditor-content", "regexp", 0), delay: 2);
            _instance.CtrlV(tweet);
            _instance.HeClick(("button", "data-testid", "tweetButton", "regexp", 0), delay: 2);
            
            try
            {
                var toast = _instance.HeGet(("*", "data-testid", "toast", "regexp", 0));
                _project.log(toast);
            }
            catch (Exception ex)
            {
                _log.Warn(ex.Message, thrw: true);
            }
        }
        

        /// <summary>
        /// Генерация ответа на твит таргет-аккаунта
        /// </summary>
        /// <param name="tweetText">Текст твита на который отвечаем</param>
        /// <param name="withNewsContext">Использовать контекст из новостной статьи</param>
        public string GenerateReply(string tweetText, bool withNewsContext = false)
        {
            var ai = new Api.AI(_project, "aiio", "meta-llama/Llama-3.3-70B-Instruct", false);
            var bio = _project.DbGet("bio", "_profile");
            
            // Рандомизация подхода к ответу
            var replyApproaches = new[] {
                "Agree with the main point and add your own specific example or observation",
                "Share a personal experience that relates to what they said",
                "Ask a thoughtful follow-up question that shows you engaged with their point",
                "Offer a different perspective or counterpoint in a friendly way",
                "Add a specific detail or fact that builds on their observation",
                "Share why this resonates with you personally"
            };
            var randomApproach = replyApproaches[new Random().Next(replyApproaches.Length)];
            
            string system;
            string user;
            
            if (withNewsContext)
            {
                var randomNews = Rnd.RndFile(Path.Combine(_project.Path, ".data", "news"), "json");
                _project.ToJson(File.ReadAllText(randomNews));
                var article = _project.Json.FullText;
                
                system = $@"You are an individual with your own perspective. Your personality: {bio}

                Reply approach: {randomApproach}

                Your task: Write a thoughtful reply to the tweet below, optionally using relevant context from the article.

                CRITICAL REQUIREMENTS:
                - Maximum 280 characters total
                - Write naturally in first person (""I think"", ""I've noticed"", ""In my experience"")
                - Sound like a REAL conversation - like replying to a friend's post
                - Reference specific details when relevant (from tweet or article)
                - NO generic reactions: avoid ""Great point!"", ""This is so true!"", ""Couldn't agree more!""
                - NO marketing language: avoid ""revolutionizing"", ""game-changing"", ""unlocking""
                - NO hashtags or @mentions - they're added automatically
                - Be genuine - ask questions, share observations, add value to the conversation
                - Vary your phrasing naturally

                Return ONLY a clean JSON object:
                {{
                  ""reply"": ""your reply text here""
                }}

                No markdown formatting, no extra text, just the JSON.";

                        user = $@"Article context (use if relevant):
                {article}

                Tweet to reply to:
                {tweetText}";
            }
            else
            {
                system = $@"You are an individual with your own perspective. Your personality: {bio}

                Reply approach: {randomApproach}

                Your task: Write a thoughtful reply to the tweet below.

                CRITICAL REQUIREMENTS:
                - Maximum 280 characters total
                - Write naturally in first person (""I think"", ""I've noticed"", ""In my experience"")
                - Sound like a REAL conversation - like replying to a friend's post
                - Engage meaningfully with what they said - don't just agree
                - NO generic reactions: avoid ""Great point!"", ""This is so true!"", ""Couldn't agree more!""
                - NO marketing language: avoid ""revolutionizing"", ""game-changing"", ""unlocking""
                - NO hashtags or @mentions - they're added automatically
                - Be genuine - share your take, ask questions, add specific observations
                - Vary your phrasing naturally - don't start every reply the same way

                Return ONLY a clean JSON object:
                {{
                  ""reply"": ""your reply text here""
                }}

                No markdown formatting, no extra text, just the JSON.";

                        user = $@"Tweet to reply to:
                {tweetText}";
            }
            
            string result = ai.Query(system, user)
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();
            
            _project.ToJson(result);
            _log.Send($"Reply generated: approach={randomApproach}, len={_project.Json.reply.ToString().Length}");
            
            return result;
        }
                
        /// <summary>
        /// Найти случайный ОРИГИНАЛЬНЫЙ твит таргет-аккаунта (исключая репосты, с фильтром по дате)
        /// </summary>
        private (HtmlElement Element, string Text) GetRandomOriginalTweet(string targetAccount, int maxDaysOld = 7)
        {
            _project.Deadline();
            
            // Небольшой скролл для загрузки твитов
            for (int i = 0; i < 2; i++)
            {
                _instance.ScrollDown();
                Thread.Sleep(1000);
            }
            
            var wall = _instance.ActiveTab.FindElementsByAttribute("div", "data-testid", "cellInnerDiv", "regexp").ToList();
            
            // Извлекаем username из URL если не передан
            if (string.IsNullOrEmpty(targetAccount))
            {
                var url = _instance.ActiveTab.URL;
                targetAccount = Regex.Match(url, @"https://x\.com/([^/]+)").Groups[1].Value;
            }
            
            var originalTweets = new List<(HtmlElement Element, string Text, DateTime Date)>();
            var now = DateTime.Now;
            
            foreach (HtmlElement cell in wall)
            {
                // Проверяем что это пост от нужного аккаунта
                var userNameElement = cell.FindChildByAttribute("div", "data-testid", "User-Name", "regexp", 0);
                if (userNameElement.IsVoid) continue;
                
                var userName = userNameElement.InnerText;
                
                // Пропускаем если это не наш таргет
                if (!userName.Contains($"@{targetAccount}")) continue;
                
                // Проверяем что это НЕ репост
                var cellText = cell.InnerText;
                if (cellText.Contains("Reposted") || cellText.Contains("retweeted")) continue;
                
                var retweetIcon = cell.FindChildByAttribute("*", "data-testid", "socialContext", "regexp", 0);
                if (!retweetIcon.IsVoid && retweetIcon.InnerText.Contains("Reposted")) continue;
                
                // Парсим дату твита
                var timeElement = cell.FindChildByTag("time", 0);
                if (timeElement.IsVoid) continue;
                
                var dateTimeStr = timeElement.GetAttribute("datetime");
                if (string.IsNullOrEmpty(dateTimeStr)) continue;
                
                DateTime tweetDate;
                if (!DateTime.TryParse(dateTimeStr, out tweetDate)) continue;
                
                // Проверяем что твит не старше N дней
                var daysOld = (now - tweetDate).TotalDays;
                if (daysOld > maxDaysOld)
                {
                    _log.Send($"Tweet too old: {daysOld:F1} days (max {maxDaysOld})");
                    continue;
                }
                
                // Получаем текст твита
                var tweetText = cell.FindChildByAttribute("div", "data-testid", "tweetText", "regexp", 0);
                if (!tweetText.IsVoid && !string.IsNullOrWhiteSpace(tweetText.InnerText))
                {
                    originalTweets.Add((cell, tweetText.InnerText, tweetDate));
                }
            }
            
            _project.Deadline(30);
            
            if (originalTweets.Count == 0)
            {
                _log.Warn($"No recent original tweets found from @{targetAccount} (max {maxDaysOld} days old)");
                throw new Exception($"No recent tweets found from @{targetAccount}");
            }
            
            // Выбираем случайный свежий твит
            Random rand = new Random();
            var selected = originalTweets[rand.Next(originalTweets.Count)];
            
            _log.Send($"Selected tweet from @{targetAccount}: {selected.Date:yyyy-MM-dd HH:mm}, text: {selected.Text.Substring(0, Math.Min(50, selected.Text.Length))}...");
            return (selected.Element, selected.Text);
        }

        /// <summary>
        /// Ответить на случайный оригинальный твит таргет-аккаунта
        /// </summary>
        public string ReplyToRandomTweet(string targetAccount = null, bool withNewsContext = false, int maxDaysOld = 7)
        {
            // Если указан аккаунт - переходим на него
            if (!string.IsNullOrEmpty(targetAccount))
            {
                _instance.Go($"https://x.com/{targetAccount}");
                Thread.Sleep(2000);
            }
            
            // Получаем случайный оригинальный твит
            var selectedTweet = GetRandomOriginalTweet(targetAccount, maxDaysOld);
            
            // Генерируем ответ
            GenerateReply(selectedTweet.Text, withNewsContext);
            string reply = _project.Json.reply.ToString();
            
            // Кликаем reply на выбранном твите
            var replyButton = selectedTweet.Element.FindChildByAttribute("button", "data-testid", "reply", "regexp", 0);
            if (replyButton.IsVoid)
            {
                _log.Warn("Reply button not found");
                throw new Exception("Reply button not found");
            }
            
            _instance.HeClick(replyButton, emu: 1);
            Thread.Sleep(2000);
            
            // Вставляем ответ
            _instance.JsSet("[data-testid='tweetTextarea_0']", reply);
            Thread.Sleep(1000);
            
            // Отправляем
            _instance.HeClick(("*", "data-testid", "tweetButton", "regexp", 0), emu:1);
            
            _log.Send($"Reply posted: len={reply.Length}");
            return reply;
        }

        /// <summary>
        /// Quote случайного оригинального твита таргет-аккаунта
        /// </summary>
        public string QuoteRandomTweet(string targetAccount = null, bool withNewsContext = false, int maxDaysOld = 7)
        {
            // Если указан аккаунт - переходим на него
            if (!string.IsNullOrEmpty(targetAccount))
            {
                _instance.Go($"https://x.com/{targetAccount}");
                Thread.Sleep(2000);
            }
            
            // Получаем случайный оригинальный твит
            var selectedTweet = GetRandomOriginalTweet(targetAccount, maxDaysOld);
            
            // Генерируем комментарий
            GenerateReply(selectedTweet.Text, withNewsContext);
            string quote = _project.Json.reply.ToString();
            
            // Кликаем retweet на выбранном твите
            var retweetButton = selectedTweet.Element.FindChildByAttribute("button", "data-testid", "retweet", "regexp", 0);
            if (retweetButton.IsVoid)
            {
                _log.Warn("Retweet button not found");
                throw new Exception("Retweet button not found");
            }
            
            _instance.HeClick(retweetButton, emu: 1);
            Thread.Sleep(1000);
            
            // Кликаем "Quote" в выпадающем меню
            _instance.HeClick(("a", "href", "https://x.com/compose/post", "regexp", 0));
            Thread.Sleep(1000);
            
            // Вставляем комментарий
            _instance.JsSet("[data-testid='tweetTextarea_0']", quote);
            Thread.Sleep(1000);
            
            // Отправляем
            
            _instance.HeClick(("*", "data-testid", "tweetButton", "regexp", 0), emu:1);
            
            _log.Send($"Quote posted: len={quote.Length}");
            return quote;
        }
        
        /// <summary>
        /// Bio Generation
        /// </summary>
        public string GenerateSimpleBio()
        {
            var professions = new[] {
                "Software developer", "Graphic designer", "Marketing specialist",
                "Teacher", "Freelance writer", "Product manager", "Data analyst",
                "UX designer", "Sales professional", "Consultant", "Engineer",
                "Content creator", "Small business owner", "Photographer"
            };

            var hobbies = new[] {
                "Coffee enthusiast", "Runner", "Photography lover", "Gamer",
                "Avid reader", "Fitness junkie", "Foodie", "Travel lover",
                "Dog person", "Cat person", "Music fan", "Podcast addict",
                "Movie buff", "Sports fan", "Amateur chef", "Plant parent"
            };

            var extras = new[] {
                "Opinions my own", "Always learning", "Weekend warrior",
                "Just here for the vibes", "Dad jokes enthusiast",
                "Lifelong learner", "Curious about everything"
            };
    
            var emojis = new[] { "☕", "🐕", "🐱", "📸", "🎮", "📚", "🏃", "🌱", "🎵", "🍕" };

            var rand = new Random();
    
            // Паттерн 1: Profession. Hobby. Extra
            if (rand.Next(0, 2) == 0)
            {
                var emoji = rand.Next(0, 100) > 60 ? " " + emojis[rand.Next(emojis.Length)] : "";
                return $"{professions[rand.Next(professions.Length)]}. {hobbies[rand.Next(hobbies.Length)]}. {extras[rand.Next(extras.Length)]}{emoji}";
            }
            // Паттерн 2: Profession | Hobby | Extra
            else
            {
                var emoji = rand.Next(0, 100) > 60 ? " " + emojis[rand.Next(emojis.Length)] : "";
                return $"{professions[rand.Next(professions.Length)]} | {hobbies[rand.Next(hobbies.Length)]} | {extras[rand.Next(extras.Length)]}{emoji}";
            }
        }
        public string GenerateNormalBioAI()
        {
            var ai = new Api.AI(_project, "aiio", "meta-llama/Llama-3.3-70B-Instruct", false);
    
            // Рандомные constraints для разнообразия
            var professionTypes = new[] { 
                "tech industry", "creative field", "education", "healthcare", 
                "business", "trades", "retail", "freelance work" 
            };
            var tones = new[] { 
                "casual and funny", "professional", "minimalist", 
                "enthusiastic", "laid-back" 
            };
            var focuses = new[] {
                "hobbies and interests", "family life", "career growth",
                "location and lifestyle", "pop culture references"
            };
    
            var rand = new Random();
            var profType = professionTypes[rand.Next(professionTypes.Length)];
            var tone = tones[rand.Next(tones.Length)];
            var focus = focuses[rand.Next(focuses.Length)];
    
            string system = $@"Generate a realistic Twitter bio for a regular person in {profType}.

Style: {tone}
Focus on: {focus}

CRITICAL REQUIREMENTS:
- Maximum 160 characters
- Sound like a REAL person, not a crypto bot
- Include 2-3 diverse elements: profession, hobbies, location, personal details
- Use simple, everyday language
- Can include 1-2 emojis (optional)
- NO crypto/NFT/blockchain/DeFi/Web3
- NO fancy titles: maven, guru, ninja, connoisseur
- BE CREATIVE - avoid common phrases like ""coffee addict"", ""always learning""

Return ONLY:
{{
  ""bio"": ""your bio text here""
}}";

            string user = "Generate ONE unique bio.";
    
            string result = ai.Query(system, user)
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();
    
            _project.ToJson(result);
            return _project.Json.bio.ToString();
        }
        public string GenerateNormalBio()
        {
            var rand = new Random();
    
            // 50% AI генерация, 50% hardcoded рандом
            if (rand.Next(0, 100) > 50)
            {
                return GenerateNormalBioAI();
            }
            else
            {
                return GenerateSimpleBio(); // Твой hardcoded метод
            }
        }
        /// <summary>
        /// Проверка bio на палево. Возвращает true если нужно менять
        /// </summary>
        public bool ShouldChangeBio(string bio)
        {
            if (string.IsNullOrWhiteSpace(bio)) return true;
    
            var bioLower = bio.ToLower();
            int score = 0;
    
            // Crypto keywords (+30 каждое)
            string[] cryptoWords = { 
                "crypto", "nft", "defi", "blockchain", "web3", "metaverse",
                "staking", "yield", "farming", "degen", "token", "dapp",
                "ethereum", "bitcoin", "solana", "polygon"
            };
            foreach (var word in cryptoWords)
                if (bioLower.Contains(word)) score += 30;
    
            // Bot titles (+25 каждое)
            string[] botTitles = { 
                "maven", "guru", "connoisseur", "ninja", "wizard", "alchemist",
                "yakuza", "samurai", "prophet", "visionary", "maestro", "sensei"
            };
            foreach (var title in botTitles)
                if (bioLower.Contains(title)) score += 25;
    
            // Marketing phrases (+15 каждое)
            string[] marketing = { 
                "navigating", "exploring", "championing", "revolutionizing",
                "unlocking", "maximizing", "optimizing", "curating", "crafting"
            };
            foreach (var phrase in marketing)
                if (bioLower.Contains(phrase)) score += 15;
    
            _log.Send($"Bio score: {score} - {(score >= 30 ? "CHANGE" : "OK")}");
    
            return score >= 30;
        }
    }
    
    
    
    #endregion
    
    #region Helper Classes
    
    public class TwitterGraphQLBuilder
    {
        private const string BASE_URL = "https://x.com/i/api/graphql";
        private const string QID_USER_BY_SCREENNAME = "oUZZZ8Oddwxs8Cd3iW3UEA";
        
        public static string BuildUserByScreenNameUrl(string username)
        {
            var variables = new Dictionary<string, object>
            {
                ["screen_name"] = username,
                ["withSafetyModeUserFields"] = true
            };
            
            var features = new Dictionary<string, object>
            {
                ["hidden_profile_likes_enabled"] = false,
                ["responsive_web_graphql_exclude_directive_enabled"] = true,
                ["verified_phone_label_enabled"] = false,
                ["subscriptions_verification_info_verified_since_enabled"] = true,
                ["highlights_tweets_tab_ui_enabled"] = true,
                ["creator_subscriptions_tweet_preview_api_enabled"] = true,
                ["responsive_web_graphql_skip_user_profile_image_extensions_enabled"] = false,
                ["responsive_web_graphql_timeline_navigation_enabled"] = true
            };
            
            string varsJson = Newtonsoft.Json.JsonConvert.SerializeObject(variables);
            string featJson = Newtonsoft.Json.JsonConvert.SerializeObject(features);
            
            return $"{BASE_URL}/{QID_USER_BY_SCREENNAME}/UserByScreenName?variables={Uri.EscapeDataString(varsJson)}&features={Uri.EscapeDataString(featJson)}";
        }
    }
    
    #endregion

   
}






