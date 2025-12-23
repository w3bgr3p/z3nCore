using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public static class TaskManager
    {
        private static Dictionary<string, string> SettingsForDb( string taskXml)
        {
            XDocument doc = XDocument.Parse(taskXml);

            var settingsDict = new Dictionary<string, string>();

            foreach (var setting in doc.Descendants("InputSetting"))
            {
                var outputVar = setting.Element("OutputVariable")?.Value;
                var value = setting.Element("Value")?.Value ?? "";

                if (!string.IsNullOrWhiteSpace(outputVar))
                {
                    var cleanVar = outputVar.Replace("{-Variable.", "").Replace("-}", "");
                    settingsDict[cleanVar] = value;
                }
            }

            settingsDict["settings_xml"] = taskXml.ToBase64();
            return settingsDict;

        }
        private static string LoadTaskSettings(IZennoPosterProjectModel project, string taskId, string tableName = "!settings")
        {

            var xmlBase64 = project.DbGet("settings_xml", tableName, where: $"task_id = '{taskId}'");
            var xml = xmlBase64.FromBase64();

            XDocument doc = XDocument.Parse(xml);
            
            foreach (var setting in doc.Descendants("InputSetting"))
            {
                var outputVar = setting.Element("OutputVariable")?.Value;

                if (!string.IsNullOrWhiteSpace(outputVar))
                {
                    var cleanVar = outputVar.Replace("{-Variable.", "").Replace("-}", "");


                    var dbValue = project.DbGet(cleanVar, tableName, where: $"task_id = '{taskId}'");

                    if (!string.IsNullOrWhiteSpace(dbValue))
                    {
                        setting.Element("Value").Value = dbValue;
                    }
                }
            }
            return doc.ToString();
        }
        private static void LoadAllSettings(IZennoPosterProjectModel project)
        {
            var taskList = project.DbGetLines("Id", "!settings", where:$"\"Id\" != ''");
            foreach (var task in taskList)
            {
                var settingsFromDb = LoadTaskSettings(project, task);
                var Id = new Guid(task.ToString());
                ZennoPoster.ImportInputSettings(Id, settingsFromDb);
            }
        }

        private static void SaveAllSettings(IZennoPosterProjectModel project)
        {
            project.ClmnAdd("Id", "!settings");
            project.ClmnAdd("Name", "!settings");
            int i = 0;
            var taskList = project.DbGetLines("Id", "!tasks", where:$"\"Id\" != ''");
            foreach (var task in taskList)
            { 
                i++;
                var name = project.DbGet("Name", $"!tasks", where:$"\"Id\" = '{task}'");
                project.DbUpd($"Id = '{task}', Name = '{name}'","!settings", log: true,where:$"id = {i}");
                
                var Id = new Guid(task.ToString());
                var settings = ZennoPoster.ExportInputSettings(Id);
                try
                {
                    var settingsDic = SettingsForDb(settings);
                    project.DicToDb(settingsDic, "!settings", log: true, where: $"id = {i}");
                }
                catch
                {
                    project.warn(settings);
                }
                
            }
            project.ClmnPrune(tblName:"!settings");

        }
        
        public static void UpdTasks(IZennoPosterProjectModel project, string tableName = "!tasks")
        {
            int i = 0;
            //var tasks = new List<string>();
            foreach (var task in ZennoPoster.TasksList)
            {
                i++;
                //project.Var("acc0",i);
                string xml = "<root>" + task + "</root>";
                XDocument doc = XDocument.Parse(xml);
                string json = JsonConvert.SerializeXNode(doc);
                
                var jObj = JObject.Parse(json)["root"];
                string cleanJson = jObj.ToString();
                //project.ToJson(json);
                project.JsonToDb(cleanJson, tableName, log:true, where: $"id = '{i}'");
            }
        }

        public static void TasksToDb(IZennoPosterProjectModel project, bool updTasks = false)
        {
            if (updTasks)UpdTasks(project);
            SaveAllSettings(project);
        }
        public static void TasksFromDb(IZennoPosterProjectModel project, bool updTasks = false)
        {
            if (updTasks)UpdTasks(project);
            LoadAllSettings(project);
        }

    }
}