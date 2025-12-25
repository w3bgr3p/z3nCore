using System;
using System.IO;
using System.Collections.Generic;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using ZennoLab.InterfacesLibrary.ProjectModel.Collections;

namespace z3nCore.Utilities
{
    public class ProfileSync
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;

        public ProfileSync(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _log = new Logger(_project, log, classEmoji: "☻");
        }

        public void RestoreProfile(
            string restoreFrom, 
            bool restoreProfile = true,
            bool restoreCookies = true,
            bool restoreInstance = true,
            bool restoreWebgl = true,
            bool rebuildWebgl = false)
        {
            _log.Send($"[DIAG] RestoreProfile START. Source='{restoreFrom}', Prof={restoreProfile}, Cook={restoreCookies}, Inst={restoreInstance}, WebGL={restoreWebgl}");
            
            restoreFrom = restoreFrom?.ToLower();
            if (restoreFrom != "folder" && restoreFrom != "zb" && restoreFrom != "zpprofile" )
                throw new Exception($"restoreFrom must be either [ folder | zb | zpprofile ]. Current: '{restoreFrom}'");
            
            var sourse = restoreFrom + "_";
            _log.Send($"[DIAG] Resolved DB prefix: '{sourse}'");

            if (restoreProfile)
            {
                var profileList = PropertyManager.GetTypeProperties(typeof(IProfile));
                var tableName = sourse + "profile";
                _log.Send($"[DIAG] Restoring IProfile from table: '{tableName}'. Properties count: {profileList?.Count ?? 0}");
                _project.SetValuesFromDb(_project.Profile, tableName, profileList);
            }

            if (restoreInstance)
            {
                var instanceList = PropertyManager.GetTypeProperties(typeof(Instance));
                var tableName = sourse + "instance";
                _log.Send($"[DIAG] Restoring Instance from table: '{tableName}'. Properties count: {instanceList?.Count ?? 0}");
                _project.SetValuesFromDb(_instance, tableName, instanceList);
            }
            
            if (restoreWebgl)
            {
                var tableName = sourse + "webgl";
                _log.Send($"[DIAG] Restoring WebGL. Rebuild={rebuildWebgl}, Table='{tableName}'");
                string webglData = (rebuildWebgl) ? _project.DbToJson(tableName) : _project.DbGet("_preferences", tableName);
                _log.Send($"[DIAG] WebGL data length: {webglData?.Length ?? 0}");
                _instance.WebGLPreferences.Load(webglData);
            }
            
            if (restoreCookies)
            {
                var tableName = sourse + "profile";
                _log.Send($"[DIAG] Restoring Cookies from table: '{tableName}'");
                var cookiesRaw = _project.DbGet($"cookies", tableName);
                _log.Send($"[DIAG] Cookies (Base64) length: {cookiesRaw?.Length ?? 0}");
                
                var cookies = cookiesRaw.FromBase64();
                _log.Send($"[DIAG] Decoded cookies length: {cookies?.Length ?? 0}");
                _instance.SetCookie(cookies);
            }
            _log.Send("[DIAG] RestoreProfile COMPLETED successfully");
        }
        
        public void SaveProfile(
            string saveTo,
            bool saveProfile = true,
            bool saveInstance = true,
            bool saveCookies = true,
            bool saveWebgl = true)
        {
            _log.Send($"[DIAG] SaveProfile START. Dest='{saveTo}', Prof={saveProfile}, Inst={saveInstance}, Cook={saveCookies}, WebGL={saveWebgl}");
            
            saveTo = saveTo?.ToLower();
            if (saveTo != "folder" && saveTo != "zb" && saveTo != "zpprofile" )
                throw new Exception($"SaveTo must be either [ folder | zb | zpprofile ]. Current: '{saveTo}'");
            
            var sourse = saveTo + "_";

            if (saveProfile)
            {
                var profileList = PropertyManager.GetTypeProperties(typeof(IProfile));
                var tableName = sourse + "profile";
                _log.Send($"[DIAG] Saving IProfile to table: '{tableName}'");
                _project.GetValuesByProperty(_project.Profile, profileList, tableToUpd: tableName);
            }

            if (saveInstance)
            {
                var instanceList = PropertyManager.GetTypeProperties(typeof(Instance));
                var tableName = sourse + "instance";
                _log.Send($"[DIAG] Saving Instance to table: '{tableName}'");
                _project.GetValuesByProperty(_instance, instanceList, tableToUpd: tableName);
            }
            
            if (saveCookies)
            {
                var tableName = sourse + "profile";
                _log.Send($"[DIAG] Saving All Cookies to table: '{tableName}'");
                _project.SaveAllCookies(_instance, table: tableName);
            }
                        
            if (saveWebgl)
            {
                var tableName = sourse + "webgl";
                _log.Send($"[DIAG] Saving WebGL to table: '{tableName}'");
                string webglData = _instance.WebGLPreferences.Save();
                _log.Send($"[DIAG] Generated WebGL string length: {webglData?.Length ?? 0}");
               
                _project.DbUpd($"_preferences = '{webglData}'", tableName, saveToVar: "");
                _project.JsonToDb(webglData, tableName);
            }
            _log.Send("[DIAG] SaveProfile COMPLETED successfully");
        }
        

        public void AddStructureToDb(bool log = false)
        {
            _log.Send("[DIAG] AddStructureToDb: Checking existing tables...");
            if (_project.TblExist("folder_profile") && _project.TblExist("zb_profile"))
            {
                _log.Send("[DIAG] Tables already exist. Skipping structure creation.");
                return;
            }
            
            string[] tables = {
                "folder_profile","folder_instance","folder_webgl", 
                "zpprofile_profile","zpprofile_instance","zpprofile_webgl", };

            string primary = "INTEGER PRIMARY KEY";
            string defaultType = "TEXT DEFAULT ''"; 
       
            var tableStructure = new Dictionary<string, string>
                {{ "id", primary },};
       
            foreach(var tablename in tables)
            {
                _log.Send($"[DIAG] Creating table: '{tablename}'");
                _project.TblAdd(tableStructure, tablename);
                _project.AddRange(tablename);   
            }

            _project.ClmnAdd("cookies","folder_profile");
            _project.ClmnAdd("cookies","zpprofile_profile");
            _project.ClmnAdd("_preferences","zpprofile_webgl");
            _project.ClmnAdd("_preferences","folder_webgl");
            
            string[] zb_tables = {"zb_profile","zb_instance"};
            string zb_primary = "TEXT PRIMARY KEY";
       
            var zb_tableStructure = new Dictionary<string, string>
                {{ "zb_id", zb_primary },{ "id", defaultType },{ "_name", defaultType }};
       
            foreach(var tablename in zb_tables)
            {
                _log.Send($"[DIAG] Creating ZB table: '{tablename}'");
                _project.TblAdd(zb_tableStructure, tablename);
            }

            _project.ClmnAdd("cookies","zb_profile");
            
            var IProfileList = z3nCore.Utilities.PropertyManager.GetTypeProperties(typeof(IProfile));
            string[] tables_profile = {
                "zpprofile_profile", "folder_profile","zb_profile",
            };

            foreach(var tablename in tables_profile)
            {
                _log.Send($"[DIAG] Adding profile columns to: '{tablename}'");
                _project.ClmnAdd("cookies",tablename);
                _project.ClmnAdd(IProfileList,tablename);
            }


            var instanceList = z3nCore.Utilities.PropertyManager.GetTypeProperties(typeof(Instance));
            string[] tables_instance = {
                "zpprofile_instance", "folder_instance","zb_instance",
            };
            foreach(var tablename in tables_instance)
            {
                _log.Send($"[DIAG] Adding instance columns to: '{tablename}'");
                _project.ClmnAdd(instanceList,tablename);
            }
            _log.Send("[DIAG] AddStructureToDb COMPLETED");
        }
    }
}