using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ConnectHub.Web.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message, string? mediaUrl = null, string? messageType = "TEXT")
        {
            // ✅ Align with production ChatHub: send single payload object with camelCase fields
            var payload = new
            {
                senderId = user,
                receiverId = (string?)null,
                content = message,
                mediaUrl = mediaUrl,
                messageType = string.IsNullOrWhiteSpace(messageType) ? "TEXT" : messageType,
                sentAt = System.DateTime.UtcNow,
                isRead = false
            };

            System.Console.WriteLine($"[Test Hub] Broadcasting ReceiveMessage: {System.Text.Json.JsonSerializer.Serialize(payload)}");
            
            // Send ONLY the payload (matching production ChatHub format)
            await Clients.All.SendAsync("ReceiveMessage", payload);
        }
    }
}
