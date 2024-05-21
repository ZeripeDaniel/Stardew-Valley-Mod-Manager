using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace StardewValley_Mod_Manager
{
    public class NexusModsOAuth
    {
        private readonly string _clientId;
        private readonly Uri _ssoUri = new Uri("wss://sso.nexusmods.com");
        private ClientWebSocket _webSocket;
        private string _uuid;

        public NexusModsOAuth(string clientId)
        {
            _clientId = clientId;
            _webSocket = new ClientWebSocket();
        }

        public async Task<string> ConnectAsync()
        {
            await _webSocket.ConnectAsync(_ssoUri, CancellationToken.None);
            _uuid = Guid.NewGuid().ToString();
            var data = new { id = _uuid, token = (string)null, protocol = 2 };
            var json = JObject.FromObject(data).ToString();
            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

            return _uuid;
        }

        public string GetAuthorizationUrl()
        {
            return $"https://www.nexusmods.com/sso?id={_uuid}&application={_clientId}";
        }

        public async Task<string> ReceiveApiKeyAsync()
        {
            var buffer = new byte[1024];
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var response = JObject.Parse(json);
            return response["data"]["api_key"].ToString();
        }
    }
}
