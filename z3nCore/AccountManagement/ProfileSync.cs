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
               
                _project.DbUpd($"_preferences = '{webglData}'",sourse + "webgl");
                _project.JsonToDb(webglData, sourse + "webgl");
            }
            
        }
        
    }
}