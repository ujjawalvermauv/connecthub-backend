using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace ConnectHub.Notification.Services
{
    public class NameIdentifierUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? connection.User?.FindFirst("sub")?.Value;
        }
    }
}