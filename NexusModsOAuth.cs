using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows;

namespace StardewValley_Mod_Manager
{
    public class NexusModsOAuth
    {
        private const string WebSocketUrl = "wss://sso.nexusmods.com";
        private ClientWebSocket webSocket;
        private string uuid;
        private string connectionToken;
        private string apiKey;
        private readonly string _gameDomainName;
        private readonly HttpClient _client;

        public NexusModsOAuth(string gameDomainName = "stardewvalley")
        {
            _gameDomainName = gameDomainName;
            _client = new HttpClient();
            uuid = Guid.NewGuid().ToString();
            webSocket = new ClientWebSocket();
        }

        public async Task ConnectAsync()
        {
            await webSocket.ConnectAsync(new Uri(WebSocketUrl), CancellationToken.None);
            await SendAuthRequestAsync();
        }

        private async Task SendAuthRequestAsync()
        {
            var data = new
            {
                id = uuid,
                token = connectionToken,
                protocol = 2
            };
            string message = JsonConvert.SerializeObject(data);
            var bytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            await ReceiveMessagesAsync();
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    var responseString = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnMessageReceived(responseString);
                }
            }
        }

        private void OnMessageReceived(string message)
        {
            var response = JsonConvert.DeserializeObject<SSOResponse>(message);
            if (response.success)
            {
                if (response.data.api_key != null)
                {
                    apiKey = response.data.api_key;
                    Console.WriteLine("API Key received: " + apiKey);
                    _client.DefaultRequestHeaders.Clear();
                    _client.DefaultRequestHeaders.Add("apikey", apiKey);
                    _client.DefaultRequestHeaders.Add("Application-Name", "StardewValleyModManager");
                    _client.DefaultRequestHeaders.Add("Application-Version", "0.0.2");
                }
                else if (response.data.connection_token != null)
                {
                    connectionToken = response.data.connection_token;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OpenUrlInPopup($"https://www.nexusmods.com/sso?id={uuid}&application=sdvmodmanager");
                    });
                }
            }
            else
            {
                Console.WriteLine("Error: " + response.error);
            }
        }

        private void OpenUrlInPopup(string url)
        {
            var popup = new WebBrowserPopup();
            popup.Navigate(url);
            popup.ShowDialog();
        }

        public async Task<string> GetLatestModVersionAsync(string modId)
        {
            var url = $"https://api.nexusmods.com/v1/games/{_gameDomainName}/mods/{modId}.json";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var latestVersion = JObject.Parse(json)["version"]?.ToString();
            return latestVersion;
        }

        public class SSOResponse
        {
            public bool success { get; set; }
            public SSOData data { get; set; }
            public string error { get; set; }
        }

        public class SSOData
        {
            public string connection_token { get; set; }
            public string api_key { get; set; }
        }
    }
}
