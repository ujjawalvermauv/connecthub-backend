using ConnectHub.Notification.Services;
using Microsoft.AspNetCore.SignalR;

namespace ConnectHub.Notification.Hubs
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