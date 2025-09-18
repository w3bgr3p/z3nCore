using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json;
namespace z3nCore
{
    public class Guild
    {
        protected readonly IZennoPosterProjectModel _project;
        protected readonly Instance _instance;
        private readonly Logger _logger;

        public Guild(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {

            _project = project;
            _instance = instance;
            _logger = new Logger(project, log: log, classEmoji: "GUILD");

        }

        public void ParseRoles(string tablename, bool append = true)
        {

            var roles = _instance.ActiveTab.FindElementsByAttribute("div", "id", "role-", "regexp").ToList();
            _project.ClmnAdd( "guild_done",tablename);
            _project.ClmnAdd( "guild_undone",tablename);

            var doneData = new List<Dictionary<string, string>>();
            var undoneData = new List<Dictionary<string, object>>();

            string doneJson = "";
            string undoneJson = "";

            if (append)
            {
                 doneJson = _project.SqlGet("guild_done", tablename);
                 undoneJson = _project.SqlGet("guild_undone", tablename);
            }

            if (!string.IsNullOrEmpty(doneJson))
            {
                doneData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(doneJson) ?? new List<Dictionary<string, string>>();
            }
            if (!string.IsNullOrEmpty(undoneJson))
            {
                undoneData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(undoneJson) ?? new List<Dictionary<string, object>>();
            }

            foreach (HtmlElement role in roles)
            {
                string name = role.FindChildByAttribute("div", "class", "flex\\ items-center\\ gap-3\\ p-5", "regexp", 0).InnerText.Trim();
                string state = role.FindChildByAttribute("div", "class", "mb-4\\ flex\\ items-center\\ justify-between\\ p-5\\ pb-0\\ transition-transform\\ md:mb-6", "regexp", 0).InnerText.Trim();
                string total = name.Split('\n')[1].Trim();
                name = name.Split('\n')[0].Trim();
                state = state.Split('\n')[1].Trim();

                if (state.Contains("No access") || state.Contains("Join Guild"))
                {
                    var tasksHe = role.FindChildByAttribute("div", "class", "flex\\ flex-col\\ p-5\\ pt-0", "regexp", 0);
                    var tasks = tasksHe.FindChildrenByAttribute("div", "class", "w-full\\ transition-all\\ duration-200\\ translate-y-0\\ opacity-100", "regexp").ToList();
                    var taskList = new List<string>();

                    foreach (HtmlElement task in tasks)
                    {
                        string taskText = task.InnerText.Split('\n')[0].Trim();
                        if (task.InnerHtml.Contains("M208.49,191.51a12,12,0,0,1-17,17L128,145,64.49,208.49a12,12,0,0,1-17-17L111,128,47.51,64.49a12,12,0,0,1,17-17L128,111l63.51-63.52a12,12,0,0,1,17,17L145,128Z"))
                        {
                            _logger.Send($"[!Undone]: {taskText}");
                            taskList.Add(taskText.Trim());
                        }
                    }
                    var undoneEntry = new Dictionary<string, object>
                    {
                        ["name"] = name,
                        ["tasks"] = taskList
                    };
                    if (!undoneData.Any(d => d["name"].ToString() == name))
                    {
                        undoneData.Add(undoneEntry);
                    }
                }
                else if (state.Contains("You have access"))
                {
                    var doneEntry = new Dictionary<string, string>
                    {
                        ["name"] = name,
                        ["total"] = total
                    };
                    if (!doneData.Any(d => d["name"] == name))
                    {
                        doneData.Add(doneEntry);
                    }
                }
                else if (state.Contains("Reconnect"))
                {
                    var undoneEntry = new Dictionary<string, object>
                    {
                        ["name"] = name,
                        ["reconnect"] = true
                    };
                    if (!undoneData.Any(d => d["name"].ToString() == name))
                    {
                        undoneData.Add(undoneEntry);
                    }
                }
            }

            string wDone = JsonConvert.SerializeObject(doneData, Formatting.Indented).Replace("'", "");
            string wUndone = JsonConvert.SerializeObject(undoneData, Formatting.Indented).Replace("'", "");
            _project.Var("guildDone", wDone);
            _project.Var("guildUndone", wUndone);
            _project.DbUpd($"guild_done = '{wDone}', guild_undone = '{wUndone}'", tablename);

        }

        public string Svg(string d)
        {
            string discord = "M108,136a16,16,0,1,1-16-16A16,16,0,0,1,108,136Zm56-16a16,16,0,1,0,16,16A16,16,0,0,0,164,120Zm76.07,76.56-67,29.71A20.15,20.15,0,0,1,146,214.9l-8.54-23.13c-3.13.14-6.27.24-9.45.24s-6.32-.1-9.45-.24L110,214.9a20.19,20.19,0,0,1-27.08,11.37l-67-29.71A19.93,19.93,0,0,1,4.62,173.41L34.15,57A20,20,0,0,1,50.37,42.19l36.06-5.93A20.26,20.26,0,0,1,109.22,51.1l4.41,17.41c4.74-.33,9.52-.51,14.37-.51s9.63.18,14.37.51l4.41-17.41a20.25,20.25,0,0,1,22.79-14.84l36.06,5.93A20,20,0,0,1,221.85,57l29.53,116.38A19.93,19.93,0,0,1,240.07,196.56ZM227.28,176,199.23,65.46l-30.07-4.94-2.84,11.17c2.9.58,5.78,1.2,8.61,1.92a12,12,0,1,1-5.86,23.27A168.43,168.43,0,0,0,128,92a168.43,168.43,0,0,0-41.07,4.88,12,12,0,0,1-5.86-23.27c2.83-.72,5.71-1.34,8.61-1.92L86.85,60.52,56.77,65.46,28.72,176l60.22,26.7,5-13.57c-4.37-.76-8.67-1.65-12.88-2.71a12,12,0,0,1,5.86-23.28A168.43,168.43,0,0,0,128,168a168.43,168.43,0,0,0,41.07-4.88,12,12,0,0,1,5.86,23.28c-4.21,1.06-8.51,1.95-12.88,2.71l5,13.57Z";



            string twitter = "M697.286 531.413 1042.75 130h-81.86L660.919 478.542 421.334 130H145l362.3 527.057L145 1078h81.87l316.776-368.072L796.666 1078H1073L697.266 531.413h.02ZM585.154 661.7l-36.708-52.483-292.077-417.612h125.747l235.709 337.026 36.709 52.483L960.928 1019.2H835.181L585.154 661.72v-.02Z";


            string github = "M212.62,75.17A63.7,63.7,0,0,0,206.39,26,12,12,0,0,0,196,20a63.71,63.71,0,0,0-50,24H126A63.71,63.71,0,0,0,76,20a12,12,0,0,0-10.39,6,63.7,63.7,0,0,0-6.23,49.17A61.5,61.5,0,0,0,52,104v8a60.1,60.1,0,0,0,45.76,58.28A43.66,43.66,0,0,0,92,192v4H76a20,20,0,0,1-20-20,44.05,44.05,0,0,0-44-44,12,12,0,0,0,0,24,20,20,0,0,1,20,20,44.05,44.05,0,0,0,44,44H92v12a12,12,0,0,0,24,0V192a20,20,0,0,1,40,0v40a12,12,0,0,0,24,0V192a43.66,43.66,0,0,0-5.76-21.72A60.1,60.1,0,0,0,220,112v-8A61.5,61.5,0,0,0,212.62,75.17ZM196,112a36,36,0,0,1-36,36H112a36,36,0,0,1-36-36v-8a37.87,37.87,0,0,1,6.13-20.12,11.65,11.65,0,0,0,1.58-11.49,39.9,39.9,0,0,1-.4-27.72,39.87,39.87,0,0,1,26.41,17.8A12,12,0,0,0,119.82,68h32.35a12,12,0,0,0,10.11-5.53,39.84,39.84,0,0,1,26.41-17.8,39.9,39.9,0,0,1-.4,27.72,12,12,0,0,0,1.61,11.53A37.85,37.85,0,0,1,196,104Z";


            string google = "M228,128a100,100,0,1,1-22.86-63.64,12,12,0,0,1-18.51,15.28A76,76,0,1,0,203.05,140H128a12,12,0,0,1,0-24h88A12,12,0,0,1,228,128Z";


            string email = "M224,44H32A12,12,0,0,0,20,56V192a20,20,0,0,0,20,20H216a20,20,0,0,0,20-20V56A12,12,0,0,0,224,44ZM193.15,68,128,127.72,62.85,68ZM44,188V83.28l75.89,69.57a12,12,0,0,0,16.22,0L212,83.28V188Z";


            string telegram = "M231.49,23.16a13,13,0,0,0-13.23-2.26L15.6,100.21a18.22,18.22,0,0,0,3.12,34.86L68,144.74V200a20,20,0,0,0,34.4,13.88l22.67-23.51L162.35,223a20,20,0,0,0,32.7-10.54L235.67,35.91A13,13,0,0,0,231.49,23.16ZM139.41,77.52,77.22,122.09l-34.43-6.75ZM92,190.06V161.35l15,13.15Zm81.16,10.52L99.28,135.81,205.59,59.63Z";


            string farcaster = "M257.778 155.556h484.444v688.889h-71.111V528.889h-.697c-7.86-87.212-81.156-155.556-170.414-155.556-89.258 0-162.554 68.344-170.414 155.556h-.697v315.556h-71.111V155.556z";


            string world = "M491.846 156.358c-12.908-30.504-31.357-57.843-54.859-81.345-23.502-23.503-50.902-41.951-81.345-54.86C324.041 6.759 290.553 0 255.97 0c-34.523 0-68.072 6.758-99.673 20.154-30.504 12.908-57.843 31.357-81.345 54.859-23.502 23.502-41.951 50.902-54.86 81.345C6.759 187.898 0 221.447 0 255.97c0 34.523 6.758 68.071 20.154 99.672 12.908 30.504 31.357 57.843 54.859 81.345 23.502 23.502 50.902 41.951 81.345 54.859C187.959 505.181 221.447 512 256.03 512c34.523 0 68.072-6.758 99.673-20.154 30.504-12.908 57.842-31.357 81.345-54.859 23.502-23.502 41.951-50.902 54.859-81.345 13.335-31.601 20.154-65.089 20.154-99.672-.061-34.523-6.88-68.072-20.215-99.612Zm-320.875 75.561c10.655-40.916 47.918-71.177 92.183-71.177h177.73c11.447 22.102 18.753 46.153 21.615 71.177H170.971Zm291.528 48.101a206.5 206.5 0 0 1-21.615 71.177h-177.73c-44.204 0-81.467-30.261-92.183-71.177h291.528ZM108.988 108.988C148.26 69.716 200.44 48.101 255.97 48.101c55.529 0 107.709 21.615 146.981 60.887a196.255 196.255 0 0 1 3.532 3.653H263.154c-38.298 0-74.282 14.918-101.377 42.012-21.31 21.311-35.071 48.162-40.003 77.327H49.501c5.297-46.457 25.938-89.443 59.487-122.992ZM255.97 463.899c-55.53 0-107.71-21.615-146.982-60.887-33.549-33.549-54.19-76.535-59.487-122.931h72.273c4.871 29.165 18.693 56.016 40.003 77.327 27.095 27.094 63.079 42.012 101.377 42.012h143.389c-1.156 1.217-2.374 2.435-3.531 3.653-39.272 39.15-91.513 60.826-147.042 60.826Z";

            if (d.Contains(discord)) return "discord";
            if (d.Contains(twitter)) return "twitter";
            if (d.Contains(github)) return "github";
            if (d.Contains(email)) return "email";
            if (d.Contains(telegram)) return "telegram";
            if (d.Contains(farcaster)) return "farcaster";
            if (d.Contains(world)) return "world";
            if (d.Contains(google)) return "google";

            return string.Empty;
        }

        public string Svg(HtmlElement he)
        {
            string d = he.InnerHtml;

            return Svg(d);

        }

        public Dictionary<string, string> ParseConnections()
        {
            var dataHe = _instance.GetHe(("div", "class", "flex\\ flex-col\\ gap-3\\ rounded-xl\\ border\\ bg-card-secondary\\ px-4\\ py-3.5\\ mb-6", "regexp", 0)).GetChildren(false);
            var dataDic = new Dictionary<string, string>();
            foreach (HtmlElement he in dataHe)
            {

                string type = Svg(he);
                if (type != "")
                {
                    if (he.InnerText.Contains("Connect"))
                        dataDic.Add(type, "none");

                    else
                        dataDic.Add(type, he.InnerText);
                }

            }
            return dataDic;
        }

        public HtmlElement MainButton()

        {
            return _instance.GetHe(("button", "class", "font-semibold\\ inline-flex\\ items-center\\ justify-center\\ whitespace-nowrap\\ transition-colors\\ focus-visible:outline-none\\ focus-visible:ring-4\\ focus:ring-ring\\ disabled:pointer-events-none\\ disabled:opacity-50\\ text-base\\ text-ellipsis\\ overflow-hidden\\ gap-1.5\\ cursor-pointer\\ \\[&_svg]:shrink-0\\ bg-\\[hsl\\(var\\(--button-bg-subtle\\)/0\\)]\\ hover:bg-\\[hsl\\(var\\(--button-bg-subtle\\)/0.12\\)]\\ active:bg-\\[hsl\\(var\\(--button-bg-subtle\\)/0.24\\)]\\ text-\\[hsl\\(var\\(--button-foreground-subtle\\)\\)]\\ \\[--button-bg:var\\(--secondary\\)]\\ \\[--button-bg-hover:var\\(--secondary-hover\\)]\\ \\[--button-bg-active:var\\(--secondary-active\\)]\\ \\[--button-foreground:var\\(--secondary-foreground\\)]\\ \\[--button-bg-subtle:var\\(--secondary-subtle\\)]\\ \\[--button-foreground-subtle:var\\(--secondary-subtle-foreground\\)]\\ h-11\\ px-4\\ py-2\\ rounded-2xl", "regexp", 0));

        }

    }
}
