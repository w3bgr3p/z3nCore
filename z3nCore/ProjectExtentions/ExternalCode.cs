using System;
using System.Collections.Generic;
using System.Text;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;



namespace z3nCore
{
    public static partial class ProjectExtensions
    {
        
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
        public static bool RunZp(this IZennoPosterProjectModel project, string path)
        {
            var vars = new List<string> {
                "acc0", "cfgLog", "cfgPin",
                "DBmode", "DBpstgrPass", "DBpstgrUser", "DBsqltPath",          
                "instancePort",  "lastQuery",
                "projectScript", "varSessionId", "wkMode",
            };
            
            
            var mapVars = new List<Tuple<string, string>>();
            if (vars != null)
                foreach (var v in vars)
                    mapVars.Add(new Tuple<string, string>(v, v)); 
            return project.ExecuteProject(path, mapVars, true, true, true); 
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
        
    }

}
