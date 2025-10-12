using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json.Linq;


namespace z3nCore
{
    public static class Utils
    {
        
        public static void Clean(this IZennoPosterProjectModel project, Instance instance)
        {
            bool releaseResouses = true;
            try { releaseResouses = project.Var("forceReleaseResouses") == "True"; } catch { }
            
            if (instance.BrowserType.ToString() == "Chromium" && releaseResouses)
            {
                try { instance.Launch(ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Firefox45, false); } catch { }
                try { instance.ClearCookie(); instance.ClearCache(); } catch { }
            }

            if (!string.IsNullOrEmpty(project.Var("accRnd")))
                new FS(project).RmRf(project.Var("pathProfileFolder"));
        }
        
        
       
        

        
        public static bool RunZp(this IZennoPosterProjectModel project, List<string> vars = null)
        {
            string tempFilePath = project.Var("projectScript");
            var mapVars = new List<Tuple<string, string>>();

            if (vars != null)
                foreach (var v in vars)
                    try 
                    {
                        mapVars.Add(new Tuple<string, string>(v, v)); 
                    }
                    catch (Exception ex)
                    {
                        project.SendWarningToLog(ex.Message, true);
                        throw;
                    }
            try 
            { 
                return project.ExecuteProject(tempFilePath, mapVars, true, true, true); 
            }
            catch (Exception ex) 
            { 
                project.SendWarningToLog(ex.Message, true);
                throw;
            }
        }

        public static void SessionInfo(this IZennoPosterProjectModel project, Instance instance,bool showInZp = false,bool resetSessionId = true)
        {
            var startInfo = new StringBuilder();
            startInfo.AppendLine($"► instance with {instance.BrowserType.ToString()} started in {project.Age<string>()}");
            startInfo.AppendLine($"running {project.Var("projectScript")}");
            startInfo.AppendLine($"acc: [{project.Var("acc0")}] toDo: [{project.Var("cfgToDo")}]");
            if (!string.IsNullOrEmpty(project.Var("requiredSocial"))) startInfo.Append($" socials: [{project.Var("requiredSocial")}]");
            project.SendInfoToLog(startInfo.ToString(),showInZp);
            if (resetSessionId) project.Var("varSessionId",(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString());
        }
        
        public static string ErrorReport(this IZennoPosterProjectModel project, Instance instance, bool log = false, bool toTg = false, bool toDb = false, bool screensot = false)
        {
            return new Reporter(project,instance,log).ErrorReport(toTg, toDb, screensot);
        }
    }

}
