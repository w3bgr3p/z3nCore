using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json.Linq;

namespace z3nCore.Api
{
    public class DiscordApi
    {

        private readonly IZennoPosterProjectModel _project;
        private readonly NetHttp _http;
        private readonly Logger _logger;

        public DiscordApi(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _http = new NetHttp(_project);
            _logger = new Logger(project, log: log, classEmoji: "DS");
        }
        

        public bool ManageRole(string botToken, string guildId, string roleName, string userId, bool assignRole, [CallerMemberName] string callerName = "")
        {
            Thread.Sleep(1000);
            try
            {
                var headers = new Dictionary<string, string>
                {
                    { "Authorization", $"Bot {botToken}" },
                    { "User-Agent", "DiscordBot/1.0" }
                };

                string rolesUrl = $"https://discord.com/api/v10/guilds/{guildId}/roles";
                string rolesResponse = _http.GET(rolesUrl, headers: headers);
                Thread.Sleep(1000);
                if (rolesResponse.StartsWith("Ошибка"))
                {
                    _logger.Send($"!W Не удалось получить роли сервера:{rolesUrl} {rolesResponse}");
                    return false;
                }

                JArray roles = JArray.Parse(rolesResponse);
                var role = roles.FirstOrDefault(r =>
                    r["name"].ToString().Equals(roleName, StringComparison.OrdinalIgnoreCase));
                if (role == null)
                {
                    _logger.Send($"!W Роль с именем '{roleName}' не найдена на сервере");
                    return false;
                }

                string roleId = role["id"].ToString();
                _logger.Send($"found : {roleName} (ID: {roleId})");

                string url = $"https://discord.com/api/v10/guilds/{guildId}/members/{userId}/roles/{roleId}";

                string result;
                if (assignRole)
                {
                    result = _http.PUT(url, "", proxyString: null, headers: headers);
                    Thread.Sleep(1000);
                }
                else
                {
                    result = _http.DELETE(url, proxyString: null, headers: headers);
                    Thread.Sleep(1000);
                }

                if (result.StartsWith("Ошибка"))
                {
                    _logger.Send($"!W Не удалось {(assignRole ? "выдать" : "удалить")} роль:{url} {result}");
                    return false;
                }

                _logger.Send(
                    $"{(assignRole ? "Роль успешно выдана" : "Роль успешно удалена")}: {roleName} для пользователя {userId}");
                return true;
            }
            catch (Exception e)
            {
                _logger.Send($"!W Ошибка при управлении ролью: [{e.Message}]");
                return false;
            }
        }
        
    }
}