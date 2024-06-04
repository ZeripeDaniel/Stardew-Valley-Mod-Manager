using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
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
        private readonly string apiKeyFilePath;

        public NexusModsOAuth(string gameDomainName = "stardewvalley")
        {
            _gameDomainName = gameDomainName;
            _client = new HttpClient();
            uuid = Guid.NewGuid().ToString();
            webSocket = new ClientWebSocket();
            apiKeyFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\StardewValleyModManager\\", "sdvmodmanager_api_key.txt");

            // API 키 파일이 존재하면 읽어오기
            if (File.Exists(apiKeyFilePath))
            {
                apiKey = File.ReadAllText(apiKeyFilePath);
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _client.DefaultRequestHeaders.Clear();
                    _client.DefaultRequestHeaders.Add("apikey", apiKey);
                    _client.DefaultRequestHeaders.Add("Application-Name", "StardewValleyModManager");
                    _client.DefaultRequestHeaders.Add("Application-Version", "0.0.2");
                }
            }
        }

        public async Task ConnectAsync()
        {
            if (string.IsNullOrEmpty(apiKey) || !(await IsApiKeyValidAsync()))
            {
                await webSocket.ConnectAsync(new Uri(WebSocketUrl), CancellationToken.None);
                await SendAuthRequestAsync();
            }
        }

        private async Task<bool> IsApiKeyValidAsync()
        {
            try
            {
                var response = await _client.GetAsync("https://api.nexusmods.com/v1/users/validate.json");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
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
                    File.WriteAllText(apiKeyFilePath, apiKey); // API 키 파일에 저장
                    _client.DefaultRequestHeaders.Clear();
                    _client.DefaultRequestHeaders.Add("apikey", apiKey);
                    _client.DefaultRequestHeaders.Add("Application-Name", "StardewValleyModManager");
                    _client.DefaultRequestHeaders.Add("Application-Version", "0.0.2");
                }
                else if (response.data.connection_token != null)
                {
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        MessageBox.Show("로그인되지 않았습니다. 다시 시도해 주세요.", "인증 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    connectionToken = response.data.connection_token;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OpenUrlInDefaultBrowser($"https://www.nexusmods.com/sso?id={uuid}&application=sdvmodmanager");
                    });
                }
            }
            else
            {
                Console.WriteLine("Error: " + response.error);
            }
        }

        private void OpenUrlInDefaultBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"웹 브라우저를 여는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
