using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

namespace PublicAPI.Common {
    public static class WebSocketHandler {
        static Dictionary<string, WebSocket> sockets = new Dictionary<string, WebSocket>();
        static Dictionary<string, string> activeTasks = new Dictionary<string, string>();
        //static readonly Dictionary<string, string> abortedSockets = new Dictionary<string, string>();

        private static string CreateMessage(string status, string message, string comment) {
            if (comment != null) {
                return JsonConvert.SerializeObject(new { status, message, comment });
            } else {
                return JsonConvert.SerializeObject(new { status, message });
            }
        }

        public static bool Notify(string taskId, int progress, string comment = null) {
            //if (abortedSockets.ContainsKey(taskId)) return false;
            if (!activeTasks.ContainsKey(taskId)) return false;
            if (sockets.ContainsKey(taskId)) NotifyInternal(taskId, progress, comment);
            return true;
        }

        public static void Start(string taskId) {
            activeTasks.Add(taskId, taskId);
        }

        public async static void Finish(string taskId, string message) {

            if (sockets.ContainsKey(taskId)) {
                WebSocket ws = sockets[taskId];
                byte[] buffer = Encoding.ASCII.GetBytes(CreateMessage("finish", message, null));
                if (ws.State == WebSocketState.Open) {
                    await ws.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                sockets.Remove(taskId);
            }
            activeTasks.Remove(taskId);

        }

        public async static void NotifyInternal(string taskId, int progress, string comment) {
            string s = CreateMessage("progress", progress.ToString(), comment);
            sockets.TryGetValue(taskId, out WebSocket ws);
            if (ws.State == WebSocketState.Open) {
                byte[] buffer = Encoding.UTF8.GetBytes(s);
                await ws.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public static async Task Handler(WebSocket webSocket, string taskId) {
            if (!activeTasks.ContainsKey(taskId)) {  // task finished before ws established
                sockets.Remove(taskId);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                return;
            }

            var buffer = new byte[1024 * 4];
            sockets[taskId] = webSocket;

            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue) {
                var msg = Encoding.Default.GetString(buffer, 0, result.Count);
                if (msg == "abort") { // echo it back and mark task as finished
                    byte[] response = Encoding.ASCII.GetBytes(CreateMessage("abort", "", null));
                    sockets.Remove(taskId);
                    //abortedSockets.Add(taskId, taskId);
                    activeTasks.Remove(taskId);
                    if (webSocket.State == WebSocketState.Open) {
                        await webSocket.SendAsync(new ArraySegment<byte>(response, 0, response.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);
                    }
                }
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
