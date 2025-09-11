using System;
using System.Net.Http;
using System.Text;


namespace z3nCore.Api
{
    public class GitHubApi
    {
        private readonly string _token;
        private readonly string _username;
        private readonly HttpClient _client;

        public GitHubApi(string token, string username)
        {
            _token = token;
            _username = username;
            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://api.github.com/");
            _client.DefaultRequestHeaders.Add("Authorization", "token " + _token);
            _client.DefaultRequestHeaders.Add("User-Agent", "GitHubManagerApp");
        }

        public string GetRepositoryInfo(string repoName)
        {
            try
            {
                var response = _client.GetAsync("repos/" + _username + "/" + repoName).Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public string GetCollaborators(string repoName)
        {
            try
            {
                var response = _client.GetAsync("repos/" + _username + "/" + repoName + "/collaborators").Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public string CreateRepository(string repoName)
        {
            try
            {
                var content = new StringContent("{\"name\":\"" + repoName + "\",\"private\":true}", Encoding.UTF8, "application/json");
                var response = _client.PostAsync("user/repos", content).Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public string ChangeVisibility(string repoName, bool makePrivate)
        {
            try
            {
                var content = new StringContent("{\"private\":" + makePrivate.ToString().ToLower() + "}", Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), "repos/" + _username + "/" + repoName) { Content = content };
                var response = _client.SendAsync(request).Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public string AddCollaborator(string repoName, string collaboratorUsername, string permission = "pull")
        {
            try
            {
                var content = new StringContent("{\"permission\":\"" + permission + "\"}", Encoding.UTF8, "application/json");
                var response = _client.SendAsync(new HttpRequestMessage(new HttpMethod("PUT"), "repos/" + _username + "/" + repoName + "/collaborators/" + collaboratorUsername) { Content = content }).Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public string RemoveCollaborator(string repoName, string collaboratorUsername)
        {
            try
            {
                var response = _client.DeleteAsync("repos/" + _username + "/" + repoName + "/collaborators/" + collaboratorUsername).Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException ex)
            {
                return "Error: " + ex.Message;
            }
        }
        
        public string ChangeCollaboratorPermission(string repoName, string collaboratorUsername, string permission = "pull")
        {
            try
            {
                if (permission != "pull" && permission != "push" && permission != "admin" && permission != "maintain" && permission != "triage")
                {
                    return "Error: Invalid permission value";
                }
                var content = new StringContent("{\"permission\":\"" + permission + "\"}", Encoding.UTF8, "application/json");
                var response = _client.SendAsync(new HttpRequestMessage(new HttpMethod("PUT"), "repos/" + _username + "/" + repoName + "/collaborators/" + collaboratorUsername) { Content = content }).Result;
                response.EnsureSuccessStatusCode();
                return response.StatusCode == System.Net.HttpStatusCode.NoContent ? "Success: Permission updated" : response.Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException ex)
            {
                return "Error: " + ex.Message;
            }
        }

    }
}