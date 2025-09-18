using Nethereum.Signer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class DMail
    {
        private string _encstring;
        private string _pid;
        private string _key;
        private readonly Logger _logger;
        private dynamic _allMail;
        private Dictionary<string, string> _headers;
        private readonly NetHttp _h;
        private readonly IZennoPosterProjectModel _project;


        //private readonly string _postRead = "https://icp.dmail.ai/api/node/v6/dmail/inbox_all/read_by_page_with_content";
        //private readonly string _postWrite = "https://icp.dmail.ai/api/node/v6/dmail/inbox_all/update_by_bulk";
        
        public DMail(IZennoPosterProjectModel project, string key = null, bool log = false)
        {
            _project = project;
            _h = new NetHttp(_project, log: log);
            _logger = new Logger(project, log: log, classEmoji: "DMail");
            _key = KeyCheck(key);
            CheckAuth();

        }



        private void Auth()
        {

            var signer = new EthereumMessageSigner();
            string key = _key;
            string wallet = _key.ToPubEvm();
            string time = string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now);

            // 1. GET nonce
            string nonceJson = _h.GET("https://icp.dmail.ai/api/node/v6/dmail/auth/generate_nonce", "");
            dynamic data_nonce = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(nonceJson);
            string nonce = data_nonce.data.nonce;

            string msgToSign = "SIGN THIS MESSAGE TO LOGIN TO THE INTERNET COMPUTER\n\n" +
                "APP NAME: \ndmail\n\n" +
                "ADDRESS: \n" + wallet + "\n\n" +
                "NONCE: \n" + nonce + "\n\n" +
                "CURRENT TIME: \n" + time;

            string sign = signer.EncodeUTF8AndSign(msgToSign, new EthECKey(key));

            // Create JObject for the message
            var message = new JObject
            {
                { "Message", "SIGN THIS MESSAGE TO LOGIN TO THE INTERNET COMPUTER" },
                { "APP NAME", "dmail" },
                { "ADDRESS", wallet },
                { "NONCE", nonce },
                { "CURRENT TIME", time }
            };

            var data = new JObject
            {
                { "message", message },
                { "signature", sign },
                { "wallet_name", "metamask" },
                { "chain_id", 1 }
            };

            string body = JsonConvert.SerializeObject(data);

            // 2. POST to verify signature
            string get_verify_json = _h.POST("https://icp.dmail.ai/api/node/v6/dmail/auth/evm_verify_signature", body);

            dynamic data_dic = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(get_verify_json);
            string encstring = data_dic.data.token;
            string pid = data_dic.data.pid;
            _logger.Send($"{encstring} {pid}");


            try
            {
                _project.Variables["encstring"].Value = encstring;
                _project.Variables["pid"].Value = pid;
            }
            catch { }

            _encstring = encstring;
            _pid = pid;


            _headers = new Dictionary<string, string>
            {
                { "dm-encstring", encstring },
                { "dm-pid",pid }
            };

        }
        public void CheckAuth()
        {
            if (!string.IsNullOrEmpty(_pid) && !string.IsNullOrEmpty(_encstring)) goto setHeaders;

            try //from project
            {
                _pid = _project.Variables["pid"].Value;
                _encstring = _project.Variables["encstring"].Value;
                if (!string.IsNullOrEmpty(_pid) && !string.IsNullOrEmpty(_encstring)) goto setHeaders;
                throw new Exception("noAuthDataFound");
            }
            catch (Exception e)
            {
                _logger.Send($"!W {e.Message}");
            }

            try //login
            {
                Auth();
                if (!string.IsNullOrEmpty(_pid) && !string.IsNullOrEmpty(_encstring)) goto setHeaders;
                throw new Exception("noAuthDataAfterLogin WTF?");
            }
            catch (Exception e)
            {
                _logger.Send($"!W {e.Message}");
            }

        setHeaders:
            if (string.IsNullOrEmpty(_pid) || string.IsNullOrEmpty(_encstring)) throw new Exception("enpty creds WTF?");

            _headers = new Dictionary<string, string>
            {
                { "dm-encstring", _encstring },
                { "dm-pid",_pid }
            };

        }
        private string KeyCheck(string key = null)
        {
            if (string.IsNullOrEmpty(key))
                key = _project.DbKey("evm");
            if (string.IsNullOrEmpty(key))
                throw new Exception("emptykey");
            return key;
        }


        public dynamic GetAll()
        {

            var pageInfo = new JObject {
        { "page", 1 },
        { "pageSize", 20 }
    };
            var data = new JObject {
        { "dm_folder", "inbox" },
        { "store_type", "mail" },
        { "pageInfo", pageInfo }
    };


            string getMsgsBody = JsonConvert.SerializeObject(data);

            string allMailJson = _h.POST("https://icp.dmail.ai/api/node/v6/dmail/inbox_all/read_by_page_with_content", getMsgsBody, headers: _headers, parse: false);

            dynamic mail = JsonConvert.DeserializeObject<ExpandoObject>(allMailJson);
            string count_items = mail.data.list.Count.ToString();
            var allMailObj = mail.data.list;

            _allMail = allMailObj;
            return allMailObj;

        }
        public Dictionary<string, string> ReadMsg(int index = 0, dynamic mail = null, bool markAsRead = true, bool trash = true)
        {
            if (mail == null)
                try
                {
                    mail = _allMail; 
                }
                catch 
                {
                    GetAll();
                    mail = _allMail;
                }

            string sender = mail[index].dm_salias.ToString();
            string date = mail[index].dm_date.ToString();

            string dm_scid = mail[index].dm_scid.ToString();
            string dm_smid = mail[index].dm_smid.ToString();

            dynamic content = mail[index].content;
            string subj = content.subject.ToString();
            string html = content.html.ToString();


            var message = new Dictionary<string, string>
            {
                { "sender", sender },
                { "date", date },
                { "subj", subj },
                { "html", html },
                { "dm_scid", dm_scid },
                { "dm_smid", dm_smid },
            };
            if (markAsRead) MarkAsRead(index, dm_scid, dm_smid);
            if (trash) Trash(index, dm_scid, dm_smid);
            return message;
        }

        public string GetUnread(bool parse = false, string key = null)
        {
            CheckAuth();
            string url = $"https://icp.dmail.ai/api2/v2/credits/getUsedMailInfo?pid={_pid}";
            _logger.Send(url);
            string GetUnread = _h.GET(url, headers: _headers, parse: true);
            if (!string.IsNullOrEmpty(key))
            {
                dynamic unrd = JsonConvert.DeserializeObject<ExpandoObject>(GetUnread);
                string[] fields = { "mail_unread_count", "message_unread_count", "not_read_count", "used_total_size" };
                if (fields.Contains(key))
                {
                    var dataDict = (IDictionary<string, object>)unrd.data;
                    return dataDict[key].ToString();
                }
                _logger.Send($"!W no object with key [{key}] in json [{GetUnread}]");
                return string.Empty;
            }

            return GetUnread;

        }
        public void Trash(int index = 0, string dm_scid = null, string dm_smid = null)
        {

            if (string.IsNullOrEmpty(dm_scid) || string.IsNullOrEmpty(dm_smid))
            {
                try
                {
                    var MessageData = ReadMsg(index);
                    MessageData.TryGetValue("dm_scid", out dm_scid);
                    MessageData.TryGetValue("dm_smid", out dm_smid);
                }
                catch
                {
                    GetAll();
                    var MessageData = ReadMsg(index);
                    MessageData.TryGetValue("dm_scid", out dm_scid);
                    MessageData.TryGetValue("dm_smid", out dm_smid);
                }
            }

            var status = new JObject {
                {"dm_folder", "trashs"}
            };

            var info = new JArray {
                new JObject {
                    {"dm_cid", dm_scid},
                    {"dm_mid", dm_smid},
                    {"dm_foldersource", "inbox"}
                }
            };

            var data = new JObject {
                {"status", status},
                {"mail_info_list", info},
                {"store_type", "mail"}
            };


            string body = JsonConvert.SerializeObject(data);
            _h.POST("https://icp.dmail.ai/api/node/v6/dmail/inbox_all/update_by_bulk", body, headers: _headers, parse: false);

        }
        public void MarkAsRead(int index = 0, string dm_scid = null, string dm_smid = null)
        {
            var status = new JObject
            {
                {"dm_is_read", 1 }
            };

            if (string.IsNullOrEmpty(dm_scid) || string.IsNullOrEmpty(dm_smid))
            {
                var MessageData = ReadMsg(index);
                MessageData.TryGetValue("dm_scid", out dm_scid);
                MessageData.TryGetValue("dm_smid", out dm_smid);
            }

            var info = new JArray
            {
                new JObject {
                    {"dm_cid", dm_scid},
                    {"dm_mid", dm_smid},
                    {"dm_foldersource", "inbox"}
                }
            };

            var data = new JObject
            {
                {"status", status},
                {"mail_info_list", info},
                {"store_type", "mail"}
            };

            string body = JsonConvert.SerializeObject(data);
            _h.POST("https://icp.dmail.ai/api/node/v6/dmail/inbox_all/update_by_bulk", body, headers: _headers, parse: false);
        }

        
    }


}
