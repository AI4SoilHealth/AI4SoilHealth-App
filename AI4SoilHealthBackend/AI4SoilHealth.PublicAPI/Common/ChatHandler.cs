using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PublicAPI.Common.Controllers;
using PublicAPI.Common.Services;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace PublicAPI.Common {
    public class Chat {
        public string message { get; set; }
        public string token { get; set; }
    }

    public class ChatBasics {
        public int id { get; set; }
        public DateTime time_created { get; set; }
    }

    public class ChatAll {
        public int id { get; set; }
        public int room_id { get; set; }
        public int person_id { get; set; }
        public string message { get; set; }
        public DateTime time_created { get; set; }

        public string name { get; set; }
    }

    public class ChatHandler {
        
        private readonly AppOptions _options;
        private readonly Db _db;
        private readonly AuthService _authService;
        private readonly CurrentUserInfo _currentUserInfo;
        private readonly ILogger<ChatHandler> _logger;
        private readonly JwtBearerOptions _jwtBearerOptions;
        //private readonly TokenValidationParameters _tokenValidationParameters;

        private static Dictionary<int, List<WebSocket>> rooms = new Dictionary<int, List<WebSocket>>();
        private static Dictionary<int, List<ChatAll>> chats = new Dictionary<int, List<ChatAll>>();

        //public ChatHandler(TokenValidationParameters tokenValidationParameters, ILogger<ChatHandler> logger, IOptions<AppOptions> options, Db db, AuthService authService, CurrentUserInfo currentUserInfo) {
        public ChatHandler(JwtBearerOptions jwtBearerOptions, ILogger<ChatHandler> logger, IOptions<AppOptions> options, Db db, AuthService authService, CurrentUserInfo currentUserInfo) {
            _options = options.Value;
            _db = db;
            _authService = authService;
            _currentUserInfo = currentUserInfo;
            _logger = logger;
            _jwtBearerOptions = jwtBearerOptions;
            //_tokenValidationParameters = tokenValidationParameters;
        }

        private ClaimsPrincipal ValidateToken(string token) {
            var handler = new JwtSecurityTokenHandler();
            //var principal = handler.ValidateToken(token, _tokenValidationParameters, out SecurityToken validatedToken);
            var principal = handler.ValidateToken(token, _jwtBearerOptions.TokenValidationParameters, out SecurityToken validatedToken);
            return principal;
        }

        private async Task BroadcastMessageToRoomAsync(ChatAll o) {
            var os = "[" + JsonConvert.SerializeObject(o) + "]";
            var messageBytes = Encoding.UTF8.GetBytes(os);
            var tasks = new List<Task>();

            foreach (var client in rooms[o.room_id].Where(c => c != null && c.State == WebSocketState.Open)) {
                tasks.Add(client.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None));
            }

            await Task.WhenAll(tasks);
        }

        public async Task Handler(WebSocket webSocket, int roomId) {

            try {
                var buffer = new byte[1024 * 4];

                if (!rooms.ContainsKey(roomId)) {
                    rooms[roomId] = new List<WebSocket>();
                    var x = await _db.QueryListAsync<ChatAll>("general.get_chats", new { RoomId = roomId }, _options.ConnectionString);
                    chats[roomId] = x;
                }
                var m = JsonConvert.SerializeObject(chats[roomId]);
                var messageBytes = Encoding.UTF8.GetBytes(m);
                await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                rooms[roomId].Add(webSocket);

                while (webSocket.State == WebSocketState.Open) {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text) {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var chat = JsonConvert.DeserializeObject<Chat>(message);
                        var claimsPrincipal = ValidateToken(chat.token);
                        if (!await _authService.SetUser(claimsPrincipal)) {
                            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Unauthorized", CancellationToken.None);
                            return;
                        }
                        var ret = await _db.QueryScalarAsync("general.save_chat", new { PersonId = _currentUserInfo.PersonId, RoomId = roomId, Message = chat.message }, _options.ConnectionString);
                        var chatBasics = JsonConvert.DeserializeObject<ChatBasics>(ret);
                        ChatAll c = new ChatAll
                        {
                            id = chatBasics.id,
                            room_id = roomId,
                            message = chat.message,
                            name = _currentUserInfo.Nickname,
                            person_id = (int)_currentUserInfo.PersonId,
                            time_created = chatBasics.time_created
                        };
                        chats[roomId].Add(c);
                        await BroadcastMessageToRoomAsync(c);
                    } else if (result.MessageType == WebSocketMessageType.Close) {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by user", CancellationToken.None);
                        rooms[roomId].Remove(webSocket);
                    }
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "Error in chat handler");
                rooms[roomId].Remove(webSocket);
            } finally {
                if (webSocket.State != WebSocketState.Closed) {
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Internal error", CancellationToken.None);
                }
                rooms[roomId].Remove(webSocket);
            }
        }
    }
}
