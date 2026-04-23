using ConnectHub.SignalR.Services;
using Microsoft.AspNetCore.SignalR;

namespace ConnectHub.SignalR.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IPresenceService _presenceService;

        public ChatHub(IPresenceService presenceService)
        {
            _presenceService = presenceService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            _presenceService.UserConnected(userId, Context.ConnectionId);

            await Clients.All.SendAsync("UserOnline", userId, _presenceService.GetConnectionCount(userId));
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();
            _presenceService.UserDisconnected(userId, Context.ConnectionId);

            if (!_presenceService.IsUserOnline(userId))
            {
                await Clients.All.SendAsync("UserOffline", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendDirectMessage(int recipientId, string content)
        {
            var senderId = GetCurrentUserId();
            var payload = new
            {
                senderId,
                recipientId,
                content,
                sentAt = DateTime.UtcNow
            };

            await Clients.User(recipientId.ToString()).SendAsync("ReceiveMessage", payload);
            await Clients.Caller.SendAsync("ReceiveMessage", payload);
        }

        public async Task TypingIndicator(int recipientId, bool isTyping)
        {
            var senderId = GetCurrentUserId();
            await Clients.User(recipientId.ToString()).SendAsync("UserTyping", senderId, isTyping);
        }

        public async Task JoinRoom(int roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
        }

        public async Task LeaveRoom(int roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
        }

        public async Task SendRoomMessage(int roomId, string content)
        {
            var senderId = GetCurrentUserId();
            var payload = new
            {
                roomId,
                senderId,
                content,
                sentAt = DateTime.UtcNow
            };

            await Clients.Group(roomId.ToString()).SendAsync("ReceiveRoomMessage", payload);
        }

        private int GetCurrentUserId()
        {
            var userIdentifier = Context.UserIdentifier;
            if (int.TryParse(userIdentifier, out var userId))
            {
                return userId;
            }

            var httpContext = Context.GetHttpContext();
            var queryValue = httpContext?.Request.Query["userId"].ToString();
            if (int.TryParse(queryValue, out userId))
            {
                return userId;
            }

            throw new HubException("Unable to resolve current user id. Provide NameIdentifier claim or query ?userId=<int>.");
        }
    }
}
