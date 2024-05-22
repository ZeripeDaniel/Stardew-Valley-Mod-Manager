using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace StardewValley_Mod_Manager
{
    public class NexusModsApi
    {
        private readonly string _apiKey;
        private readonly string _gameDomainName;
        private readonly HttpClient _client;

        public NexusModsApi(string apiKey, string gameDomainName = "stardewvalley")
        {
            _apiKey = apiKey;
            _gameDomainName = gameDomainName;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("apikey", _apiKey);
            _client.DefaultRequestHeaders.Add("Application-Name", "StardewValleyModManager");
            _client.DefaultRequestHeaders.Add("Application-Version", "0.0.1");
        }

        private void EnsureApiLogDirectory()
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "api");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        private void LogApiResponse(string endpoint, string responseContent, HttpResponseHeaders headers)
        {
            EnsureApiLogDirectory();
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "api");
            string logFilePath = Path.Combine(logDirectory, $"{endpoint.Replace("/", "_")}_{DateTime.Now:yyyyMMddHHmmss}.log");

            using (StreamWriter writer = new StreamWriter(logFilePath))
            {
                writer.WriteLine("Headers:");
                foreach (var header in headers)
                {
                    writer.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
                writer.WriteLine();
                writer.WriteLine("Response:");
                writer.WriteLine(responseContent);
            }
        }

        public async Task<string> GetModInfoAsync(int modId)
        {
            var url = $"https://api.nexusmods.com/v1/games/{_gameDomainName}/mods/{modId}.json";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync();
            LogApiResponse($"GetModInfo_{modId}", jsonResponse, response.Headers);
            return jsonResponse;
        }

        public async Task<string> GetLatestModVersionAsync(string modId)
        {
            var url = $"https://api.nexusmods.com/v1/games/{_gameDomainName}/mods/{modId}.json";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync();
            LogApiResponse($"GetLatestModVersion_{modId}", jsonResponse, response.Headers);
            var latestVersion = JObject.Parse(jsonResponse)["version"]?.ToString();
            return latestVersion;
        }
    }
}
