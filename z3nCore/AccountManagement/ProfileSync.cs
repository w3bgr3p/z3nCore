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

        public ProfileSync(IZennoPosterProjectModel project, Instance instance)
        {
            _project = project;
            _instance = instance;
        }

        public void RestoreProfile(
            string restoreFrom, 
            bool restoreProfile = true,
            bool restoreCookies = true,
            bool restoreInstance = true,
            bool restoreWebgl = true,
            bool rebuildWebgl = false)
        {
            restoreFrom = restoreFrom.ToLower();
            if (restoreFrom != "folder" && restoreFrom != "zb" && restoreFrom != "zpprofile" )
                throw new Exception("restoreFrom must be either [ folder | zb | zpprofile ] ");
            
            var sourse = restoreFrom +"_";

            if (restoreProfile)
            {
                var profileList = PropertyManager.GetTypeProperties(typeof(IProfile));
                _project.SetValuesFromDb(_project.Profile,sourse+ "profile", profileList);
            }

            if (restoreInstance)
            {
                var instanceList = PropertyManager.GetTypeProperties(typeof(Instance));
                _project.SetValuesFromDb(_instance, sourse + "instance", instanceList);
            }
            
            if (restoreWebgl)
            {
                string webglData = (rebuildWebgl) ? _project.DbToJson(sourse +"webgl") :_project.DbGet("_preferences",sourse +"webgl");
                _instance.WebGLPreferences.Load(webglData);
            }
            
            if (restoreCookies)
            {
                var cookies = _project.DbGet($"cookies",sourse + "profile").FromBase64();
                _instance.SetCookie(cookies);
            }
            
        }
        
        public void SaveProfile(
            string saveTo,
            bool saveProfile = true,
            bool saveInstance = true,
            bool saveCookies = true,
            bool saveWebgl = true)
        {
            saveTo = saveTo.ToLower();
            if (saveTo != "folder" && saveTo != "zb" && saveTo != "zpprofile" )
                throw new Exception("SaveTo must be either [ folder | zb | zpprofile ] ");
            
            var sourse = saveTo +"_";

            if (saveProfile)
            {
                var profileList = PropertyManager.GetTypeProperties(typeof(IProfile));
                _project.GetValuesByProperty(_project.Profile, profileList, tableToUpd: sourse + "profile");
            }

            if (saveInstance)
            {
                var instanceList = PropertyManager.GetTypeProperties(typeof(Instance));
                _project.GetValuesByProperty(_instance,instanceList, tableToUpd:sourse + "instance");
            }
            
            if (saveCookies)
            {
                _project.SaveAllCookies(_instance, table:sourse + "profile");
            }
                        
            if (saveWebgl)
            {
                string webglData =  _instance.WebGLPreferences.Save();
               
                _project.DbUpd($"_preferences = '{webglData}'",sourse + "webgl", saveToVar:"");
                _project.JsonToDb(webglData, sourse + "webgl");
            }
            
        }
        

        public void AddStructureToDb(  bool log = false)
        {
            if (_project.TblExist("folder_profile") && _project.TblExist("zb_profile"))
            {
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
                _project.TblAdd(zb_tableStructure, tablename);
            }

            _project.ClmnAdd("cookies","zb_profile");
            
            var IProfileList = z3nCore.Utilities.PropertyManager.GetTypeProperties(typeof(IProfile));
            string[] tables_profile = {
                "zpprofile_profile", "folder_profile","zb_profile",
            };

            foreach(var tablename in tables_profile)
            {
                _project.ClmnAdd("cookies",tablename);
                _project.ClmnAdd(IProfileList,tablename);
            }


            var instanceList = z3nCore.Utilities.PropertyManager.GetTypeProperties(typeof(Instance));
            string[] tables_instance = {
                "zpprofile_instance", "folder_instance","zb_instance",
            };
            foreach(var tablename in tables_instance)
            {
                _project.ClmnAdd(instanceList,tablename);
            }
            
        }
        
        
    }
}