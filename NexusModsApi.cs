using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace StardewValley_Mod_Manager
{
    public class NexusModsApi
    {
        private readonly string _apiKey;
        private readonly HttpClient _client;

        public NexusModsApi(string apiKey)
        {
            _apiKey = apiKey;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("apikey", _apiKey);
        }

        public async Task<string> GetModInfoAsync(string gameDomainName, int modId)
        {
            var url = $"https://api.nexusmods.com/v1/games/{gameDomainName}/mods/{modId}.json";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
