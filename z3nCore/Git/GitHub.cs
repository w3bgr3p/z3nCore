using System;
using System.Net.Http;
using System.Text;


namespace z3nCore.Api
{

     public class GitHub
    {
        #region Fields
        private readonly string _token;
        private readonly string _username;
        private readonly string _organization;
        private readonly HttpClient _client;
        #endregion

        #region Constructors
        public GitHub(string token, string username, string organization = null)
        {
            _token = token;
            _username = username;
            _organization = organization;
            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://api.github.com/");
            _client.DefaultRequestHeaders.Add("Authorization", "token " + _token);
            _client.DefaultRequestHeaders.Add("User-Agent", "GitHubManagerApp");
        }

        // Для обратной совместимости
        public GitHub(string token, string username) : this(token, username, null)
        {
        }
        #endregion

        #region Public API
        public string GetRepositoryInfo(string repoName)
        {
            try
            {
                string owner = GetOwner();
                var response = _client.GetAsync($"repos/{owner}/{repoName}").Result;
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
                string owner = GetOwner();
                var response = _client.GetAsync($"repos/{owner}/{repoName}/collaborators").Result;
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
                
                // Для организации используем другой endpoint
                string endpoint = string.IsNullOrWhiteSpace(_organization) 
                    ? "user/repos" 
                    : $"orgs/{_organization}/repos";
                
                var response = _client.PostAsync(endpoint, content).Result;
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
                string owner = GetOwner();
                var content = new StringContent("{\"private\":" + makePrivate.ToString().ToLower() + "}", Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"repos/{owner}/{repoName}") { Content = content };
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
                string owner = GetOwner();
                var content = new StringContent("{\"permission\":\"" + permission + "\"}", Encoding.UTF8, "application/json");
                var response = _client.SendAsync(
                    new HttpRequestMessage(new HttpMethod("PUT"), $"repos/{owner}/{repoName}/collaborators/{collaboratorUsername}") 
                    { Content = content }
                ).Result;
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
                string owner = GetOwner();
                var response = _client.DeleteAsync($"repos/{owner}/{repoName}/collaborators/{collaboratorUsername}").Result;
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
                
                string owner = GetOwner();
                var content = new StringContent("{\"permission\":\"" + permission + "\"}", Encoding.UTF8, "application/json");
                var response = _client.SendAsync(
                    new HttpRequestMessage(new HttpMethod("PUT"), $"repos/{owner}/{repoName}/collaborators/{collaboratorUsername}") 
                    { Content = content }
                ).Result;
                response.EnsureSuccessStatusCode();
                return response.StatusCode == System.Net.HttpStatusCode.NoContent ? "Success: Permission updated" : response.Content.ReadAsStringAsync().Result;
            }
            catch (HttpRequestException ex)
            {
                return "Error: " + ex.Message;
            }
        }
        #endregion

        #region Private Implementation
        private string GetOwner()
        {
            return string.IsNullOrWhiteSpace(_organization) ? _username : _organization;
        }
        #endregion
    }
     

}