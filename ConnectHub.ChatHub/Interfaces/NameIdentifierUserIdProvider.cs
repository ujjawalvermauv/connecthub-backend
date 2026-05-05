using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace ConnectHub.ChatHub.Interfaces
{
    public class NameIdentifierUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
            => connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? connection.User?.FindFirst("sub")?.Value;
    }
}
