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
            var resp = _project.GET(url, "+", headers, cookies, parse: true);
            return resp.StartsWith("200");
        }
        
        /// <summary>
        /// Получение информации о пользователе
        /// </summary>
        /// <param name="targetUsername">Username для поиска</param>
        /// <param name="fieldsToKeep">Какие поля вернуть (null = ToGet.Scraping по умолчанию)</param>
        public Dictionary<string, string> GetUserInfo(string targetUsername = null, string[] fieldsToKeep = null)
        {
            if (string.IsNullOrEmpty(targetUsername)) targetUsername = _login;
            _idle.Sleep();
            string cookies = MakeCookieFromDb();
            string[] headers = BuildHeaders();
    
            var url = TwitterGraphQLBuilder.BuildUserByScreenNameUrl(targetUsername);
            var resp = _project.GET(url, "+", headers, cookies);
    
            var fullResult = resp.JsonToDic();
    
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
            
            _instance.JsClick("[data-testid='SideNav_NewTweet_Button']");
            _instance.JsSet("[data-testid='tweetTextarea_0']", title);
            _idle.Sleep();

            int tIndex = 1;
            foreach (var add in tweets)
            {
                _instance.JsClick("[data-testid='addButton']");
                _idle.Sleep();
                _instance.JsSet($"[data-testid='tweetTextarea_{tIndex}']", add);
                _idle.Sleep();
                tIndex++;
            }
            
            _instance.JsClick("[data-testid='tweetButton']");
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
        
        #region Intent Links
        
        public void FollowByLink(string screen_name)
        {
            Tab tab = _instance.NewTab("twitter");
            _instance.Go($"https://x.com/intent/follow?screen_name={screen_name}");
            _idle.Sleep();
            _instance.HeGet(("button", "data-testid", "confirmationSheetConfirm", "regexp", 0));
            _instance.JsClick("[data-testid='confirmationSheetConfirm']");
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
            _instance.JsClick("[data-testid='confirmationSheetConfirm']");
            _idle.Sleep();
            tab.Close();
        }

        public void RetweetByLink(string tweet_id)
        {
            Tab tab = _instance.NewTab("twitter");
            _instance.Go($"https://x.com/intent/retweet?tweet_id={tweet_id}");
            _idle.Sleep();
            _instance.HeGet(("button", "data-testid", "confirmationSheetConfirm", "regexp", 0));
            _instance.JsClick("[data-testid='confirmationSheetConfirm']");
            _idle.Sleep();
            tab.Close();
        }

        public void LikeByLink(string tweet_id)
        {
            Tab tab = _instance.NewTab("twitter");
            _instance.Go($"https://x.com/intent/like?tweet_id={tweet_id}");
            _idle.Sleep();
            _instance.HeGet(("button", "data-testid", "confirmationSheetConfirm", "regexp", 0));
            _instance.JsClick("[data-testid='confirmationSheetConfirm']");
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
            _instance.JsClick("[data-testid='tweetButton']");
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
            var cookJson = new Cookies(_project, _instance).Get(".");
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
    public class TwitterContent_
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        
        private string _login;
        
        internal TwitterContent_(IZennoPosterProjectModel project, Instance instance, Logger log)
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
        public string Generate(string purpose = "tweet", string model = "meta-llama/Llama-3.3-70B-Instruct")
        {
            var randomNews = Rnd.RndFile(Path.Combine(_project.Path, ".data", "news"), "json");
            _project.ToJson(File.ReadAllText(randomNews));
            var article = _project.Json.FullText;
            var ai = new Api.AI(_project, "aiio", model:model, false);
            var bio = _project.DbGet("bio", "_profile");
            
            string system = "";
            if (purpose == "tweet")
                system = $"You are an individual assistant with your own background. Your way of thinking and responding: {bio}. Your task is to summarize the provided article and express your personal opinion on its subject in a single statement, no longer than 280 characters. The statement must be a self-contained insight that summarizes the article's key points, followed by a clear attribution of your bio-informed opinion on the subject using phrases like \"I think,\" \"As for me,\" or \"I suppose.\" The opinion must be explicitly yours, not a detached thesis. Your response must be a clean JSON object with a single key 'statement' containing one string, with no nesting, no additional comments, no extra characters, and no text outside the JSON structure.";
            else if (purpose == "thread") 
                system = $"You are an individual assistant with your own background. Your way of thinking and responding: {bio}. Your task is to summarize the provided article in a series of short, concise statements, each no longer than 280 characters. Start with an engaging statement to capture attention and lead into the summary. End the summary with a concluding statement that wraps up the key takeaway. Then, provide your personal opinion as a few short theses, each no longer than 280 characters. Your response must be a clean JSON object with two keys: 'summary_statements' as an array of strings for the summary parts (starting with the lead-in and ending with the conclusion), and 'opinion_theses' as an array of strings for the theses. Include no nesting beyond these arrays, no additional comments, no extra characters, and no text outside the JSON structure.";
            else if (purpose == "opinionThread") 
                system = $"You are an individual assistant with your own background. Your way of thinking and responding: {bio}. Your task is to summarize the provided article in one concise statement, no longer than 280 characters, capturing its key points. Then, provide your personal opinion on the subject in a second statement, no longer than 280 characters, using phrases like \"I think,\" \"As for me,\" or \"I suppose\" to clearly attribute it as your bio-informed perspective, not a detached thesis. Your response must be a clean JSON object with two keys: 'summary_statement' containing the summary string, and 'opinion_statement' containing the opinion string, with no nesting, no additional comments, no extra characters, and no text outside the JSON structure.";
            else
                _log.Warn($"UNKNOWN PURPOSE: {purpose}");
            
            string result = ai.Query(system, article).Replace("```json", "").Replace("```", "");
            _project.ToJson(result);
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
    }
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