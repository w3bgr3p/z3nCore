using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Diagnostics;

namespace z3nCore
{
    public static class Starter
    {
        private static string Quote(string name)
        {
            return $"\"{name.Replace("\"", "\"\"")}\"";
        }
        private static bool IsValidRange(string range)
        {
            if (string.IsNullOrEmpty(range)) return false;
            return Regex.IsMatch(range, @"^[\d\s,\-]+$");
        }
        public static bool ChooseSingleAcc(this IZennoPosterProjectModel project, bool oldest = false)
        {
            var listAccounts = project.Lists["accs"];
            string pathProfiles = project.Var("profiles_folder");

            
            if (listAccounts.Count == 0)
            {
                project.Variables["noAccsToDo"].Value = "True";
                project.SendToLog($"♻ noAccoutsAvaliable", LogType.Info, true, LogColor.Turquoise);
                project.Variables["acc0"].Value = "";
                return false;
            }

            int randomAccount = oldest ? 0 : new Random().Next(0, listAccounts.Count) ;
            string acc0 = listAccounts[randomAccount];
            project.Var("acc0", acc0);
            listAccounts.RemoveAt(randomAccount);
            //project.Var("pathProfileFolder", $"{pathProfiles}accounts\\profilesFolder\\{acc0}");
            //project.Var("pathCookies", $"{pathProfiles}accounts\\cookies\\{acc0}.json");
            project.L0g($"`working with: [acc{acc0}] accs left: [{listAccounts.Count}]");
            return true;
        }
        
        public static void PrepareInstance(this IZennoPosterProjectModel project, Instance instance)
        {
            new Init(project, instance).PrepareInstance();
        }

        public static void _SAFU(this IZennoPosterProjectModel project)
        {
            string tempFilePath = project.Path + "_SAFU.zp";
            var mapVars = new List<Tuple<string, string>>();
            mapVars.Add(new Tuple<string, string>("acc0", "acc0"));
            mapVars.Add(new Tuple<string, string>("cfgPin", "cfgPin"));
            mapVars.Add(new Tuple<string, string>("DBpstgrPass", "DBpstgrPass"));
            try { project.ExecuteProject(tempFilePath, mapVars, true, true, true); }
            catch (Exception ex) { project.SendWarningToLog(ex.Message); }
            
        }

        public static void BuildNewDatabase (this IZennoPosterProjectModel project)
        {
            if (project.Var("cfgBuldDb") != "True") return;

            string filePath = Path.Combine(project.Path, "DbBuilder.zp");
            if (File.Exists(filePath))
            {
                project.Var("projectScript",filePath);
                project.Var("wkMode","Build");
                project.Var("cfgAccRange", project.Var("rangeEnd"));
                
                var vars =  new List<string> {
                    "cfgLog",
                    "cfgPin",
                    "cfgAccRange",
                    "DBmode",
                    "DBpstgrPass",
                    "DBpstgrUser",
                    "DBsqltPath",
                    "debug",
                    "lastQuery",
                    "wkMode",
                };
                project.RunZp(vars);
            }
            else
            {
                project.SendWarningToLog($"file {filePath} not found. Last version can be downloaded by link \nhttps://raw.githubusercontent.com/w3bgrep/z3nFarm/master/DbBuilder.zp");
            }
            
        }

        public static void MakeAccList(this IZennoPosterProjectModel project, List<string> dbQueries, bool log = false)
        {

            
            
            var _logger = new Logger(project, log);
            var result = new List<string>();


            if (!string.IsNullOrEmpty(project.Variables["acc0Forced"].Value))
            {
                project.Lists["accs"].Clear();
                project.Lists["accs"].Add(project.Variables["acc0Forced"].Value);
                _logger.Send($@"manual mode on with {project.Variables["acc0Forced"].Value}");
                return;
            }

            var allAccounts = new HashSet<string>();
            foreach (var query in dbQueries)
            {
                try
                {
                    //var accsByQuery = _sql.DbQ(query).Trim();
                    var accsByQuery = project.DbQ(query,log:log).Trim();
                    if (!string.IsNullOrWhiteSpace(accsByQuery))
                    {
                        var accounts = accsByQuery.Split('\n').Select(x => x.Trim().TrimStart(','));
                        allAccounts.UnionWith(accounts);
                    }
                }
                catch
                {
                    _logger.Send($"{query}");
                }
            }

            if (allAccounts.Count == 0)
            {
                project.Variables["noAccsToDo"].Value = "True";
                _logger.Send($"♻ noAccountsAvailable by queries [{string.Join(" | ", dbQueries)}]");
                return;
            }
            _logger.Send($"Initial availableAccounts: [{string.Join(", ", allAccounts)}]");
            FilterAccList(project, allAccounts, log);
        }
        private static void FilterAccList(IZennoPosterProjectModel project, HashSet<string> allAccounts, bool log = false)
        {
            var _logger = new Logger(project, log);

            if (!string.IsNullOrEmpty(project.Variables["requiredSocial"].Value))
            {
                string[] demanded = project.Variables["requiredSocial"].Value.Split(',');
                _logger.Send($"Filtering by socials: [{string.Join(", ", demanded)}]");

                foreach (string social in demanded)
                {
                    string tableName = $"projects_{social.Trim().ToLower()}";
                    var notOK = project.SqlGet($"id", tableName, where: "status LIKE '%suspended%' OR status LIKE '%restricted%' OR status LIKE '%ban%' OR status LIKE '%CAPTCHA%' OR status LIKE '%applyed%' OR status LIKE '%Verify%'", log: log)
                        .Split('\n')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x));
                    allAccounts.ExceptWith(notOK);
                    _logger.Send($"After {social} filter: [{string.Join("|", allAccounts)}]");
                }
            }
            project.Lists["accs"].Clear();
            project.Lists["accs"].AddRange(allAccounts);
            _logger.Send($"final list [{string.Join("|", project.Lists["accs"])}]");

        }
        
        public static List<string> ToDoQueries(this IZennoPosterProjectModel project, string toDo = null, string defaultRange = null, string defaultDoFail = null)
        {
            string tableName = project.Variables["projectTable"].Value;

            var nowIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            if (string.IsNullOrEmpty(toDo)) 
                toDo = project.Variables["cfgToDo"].Value;
            
            var toDoItems = new List<string>();

            foreach (string task in toDo.Split(','))
            { 
                toDoItems.Add(task.Trim());
            }
            
            
            string customTask  = project.Var("cfgCustomTask");
            if (!string.IsNullOrEmpty(customTask))
                toDoItems.Add(customTask);
            

            var allQueries = new List<(string TaskId, string Query)>();

            foreach (string taskId in toDoItems)
            {
                string trimmedTaskId = taskId.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedTaskId))
                {
                    string range = defaultRange ?? project.Variables["range"].Value;
                    string doFail = defaultDoFail ?? project.Variables["doFail"].Value;
                    project.ClmnAdd(trimmedTaskId,tableName);
                    string failCondition = (doFail != "True" ? "AND status NOT LIKE '%fail%'" : "");
                    string query = $@"SELECT {Quote("id")} 
                              FROM {Quote(tableName)} 
                              WHERE {Quote("id")} in ({range}) {failCondition} 
                              AND {Quote("status")} NOT LIKE '%skip%' 
                              AND ({Quote(trimmedTaskId)} < '{nowIso}' OR {Quote(trimmedTaskId)} = '')";
                    allQueries.Add((trimmedTaskId, query));
                }
            }

            return allQueries
                .OrderBy(x =>
                {
                    if (string.IsNullOrEmpty(x.TaskId))
                        return DateTime.MinValue;

                    if (DateTime.TryParseExact(
                            x.TaskId,
                            "yyyy-MM-ddTHH:mm:ss.fffZ",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                            out DateTime parsed))
                    {
                        return parsed;
                    }

                    // fallback: если парсинг не удался
                    return DateTime.MinValue;
                })
                .Select(x => x.Query)
                .ToList();
        }

        public static void BlockchainFilter (this IZennoPosterProjectModel project)
        {
            //gasprice
            if (!string.IsNullOrEmpty(project.Var("acc0Forced")) ) return ;
            string[] chains = project.Var("gateOnchainChain").Split(',');
            
            if (project.Var("gateOnchainMaxGas") != "")
            {
                decimal maxGas = decimal.Parse(project.Var("gateOnchainMaxGas"));
                foreach (string chain in chains)
                {
                    decimal gas = W3bTools.GasPrice(Rpc.Get(chain));
                    if (gas >= maxGas) 
                        goto native;
                }
                throw new Exception($"gas is over the limit: {maxGas} on {project.Var("gateOnchainChain")}");
            }
            
            native:
            //native
            if( project.Var("gateOnchainMinNative") != "")
            {
	        
                decimal minNativeInUsd = decimal.Parse(project.Var("gateOnchainMinNative"));
                string tiker;
                string  address = project.DbGet("evm_pk","_addresses", log:false);

                decimal native = 0;
                foreach (string chain in chains)
                {
                    native = project.EvmNative(Rpc.Get(chain),address);
                    project.SendInfoToLog($"{native}");
                    switch(chain)
                    {
			        
                        case "bsc":
                        case "opbnb":
                            tiker = "BNB";
                            break;
                        case "solana":
                            address = project.DbGet("sol","public_blockchain", log:false);
                            native = W3bTools.SolNative(Rpc.Get(chain),address);
                            tiker = "SOL";
                            break;
                        case "avalance":
                            tiker = "AVAX";
                            break;
                        default:			
                            tiker = "ETH";
                            break;
                    }
                    var required = project.Var("nativeBy") == "native" ? minNativeInUsd : project.UsdToToken(minNativeInUsd, tiker,"OKX");
                    if (native >= required) { 			
                        project.SendToLog($"{address} have sufficient [{native}] native in {chain}",LogType.Info ,true ,LogColor.LightBlue);
                        return ;
                    }
                    project.SendWarningToLog($"!W no balnce required: [{required}${tiker}] in  {chain}. native is [{native}] for {address}");
                }
                project.DbUpd($"status = '! noBalance', daily = '{Time.Cd(60)}'");
                throw new Exception($"!W no balnce required: [{minNativeInUsd}] in chains {project.Var("gateOnchainChain")}");
            }
        }

        public static void SocialFilter(this IZennoPosterProjectModel project)
        {
            if (string.IsNullOrEmpty(project.Var("requiredSocial"))) return ;
            
            var requiredSocials = project.Var("requiredSocial").Split(',').ToList();
            var badList = new List<string>{
                "suspended",
                "restricted",
                "ban",
                "CAPTCHA",
                "applyed",
                "Verify"};
            
            foreach (var social in requiredSocials)
            {
                var tableName = "_" + social.ToLower().Trim();
                var status = project.DbGet("status",tableName, log:true);
                foreach (string word in badList)
                {
                    if (status.Contains(word)){
                        string exMsg = $"{social} of {project.Var("acc0")}: [{status}]";
                        project.SendWarningToLog(exMsg);
                        throw new Exception(exMsg);
                    }
                }
            }


        }

        public static void GetAccByMode(this IZennoPosterProjectModel project)
        {
            

            project.L0g("accsQueue: " + string.Join(", ",project.Lists["accs"]));

            if (project.Var("wkMode") == "Cooldown") //Cooldown
            {
                //chose:
                bool chosen = project.ChooseSingleAcc();
	
                if (!chosen)
                {
                    project.Var("acc0", null);
                    project.Var("TimeToChill", "True");

                    throw new Exception($"TimeToChill");
                }
            }

            if (project.Var("wkMode") == "Oldest") //Cooldown
            {

                bool chosen = project.ChooseSingleAcc(true);
	
                if (!chosen)
                {
                    project.Var("acc0", null);
                    project.Var("TimeToChill", "True");
                    throw new Exception($"TimeToChill");
                }
            }


            if (project.Var("wkMode") == "NewRandom") //DeadSouls
            {
                string toSet = new Sql(project).GetRandom("proxy, webgl","private_profile",log:false, acc:true);
                string acc0 = toSet.Split('|')[0];
                project.Var("accRnd", Rnd.RndHexString(64));
                project.Var("acc0", acc0);
                project.Var("pathProfileFolder",project.Var("profiles_folder") + "accounts\\profilesFolder\\" + project.Var("accRnd"));
            }
            

            if (project.Var("wkMode") == "UpdateToken") //UpdateToken
            {
                string toSet = new Sql(project).GetRandom("token",log:true, acc:true, invert:true);
                string acc0 = toSet.Split('|')[0];
                project.Var("acc0", acc0);
                project.Var("pathProfileFolder", project.Var("profiles_folder") + "accounts\\profilesFolder\\" + project.Var("acc0"));
            }
            
            if (string.IsNullOrEmpty(project.Var("acc0"))) //Default
            {
                project.L0g($"acc0 is empty. Check {project.Var("wkMode")} conditions maiby it's TimeToChill",thr0w:true);
            }
            
        }

        public static void InitProject(this IZennoPosterProjectModel project, string author = "w3bgr3p", string[] customQueries = null, bool log = false )
        {
            project._SAFU();
            new Init(project, false).InitVariables(author); 
            project.BuildNewDatabase();

            project.TblPrepareDefault();
            var allQueries = project.ToDoQueries();
            
            if (customQueries != null)
                foreach(var query in customQueries) 
                    allQueries.Add(query);

            if (allQueries.Count > 0) 
                project.MakeAccList(allQueries, log: true);
            else 
                project.L0g($"unsupported SQLFilter: [{project.Variables["wkMode"].Value}]",thr0w:true);

            
        }
        
        public static bool RunProject(this IZennoPosterProjectModel project, List<string> additionalVars = null, bool add = true )
        {
            string pathZp = project.Var("projectScript");
            var vars =  new List<string> {
                "acc0",
                "accRnd",
                "cfgChains",
                "cfgRefCode",
                "captchaModule",
                "cfgDelay",
                "cfgLog",
                "cfgPin",
                "cfgToDo",
                "DBmode",
                "DBpstgrPass",
                "DBpstgrUser",
                "DBsqltPath",
                "failReport",
                "humanNear",          
                "instancePort", 
                "ip",
                "run",
                "lastQuery",
                "lastErr",
                "pathCookies",
                "projectName",
                "projectTable", 
                "projectScript",
                "proxy",          
                "requiredSocial",
                "requiredWallets",
                "toDo",
                "varSessionId",
                "wkMode",
            };

            if (additionalVars != null)
            {
                if (add)
                {
                    foreach (var varName in additionalVars)
                        if (!vars.Contains(varName)) vars.Add(varName);
                }
                else
                {
                    vars = additionalVars;
                }

            }

            project.L0g($"running {pathZp}" );
            
            var mapVars = new List<Tuple<string, string>>();
            if (vars != null)
                foreach (var v in vars)
                    mapVars.Add(new Tuple<string, string>(v, v)); 
            return project.ExecuteProject(pathZp, mapVars, true, true, true); 
        }
    }
}