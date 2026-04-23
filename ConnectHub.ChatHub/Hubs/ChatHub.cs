using ConnectHub.ChatHub.Services;
using ConnectHub.Message.Services;
using MessageEntity = ConnectHub.Message.Models.Message;
using Microsoft.AspNetCore.SignalR;

namespace ConnectHub.ChatHub.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IChatRoomService _chatRoomService;
        private readonly IPresenceService _presenceService;

        public ChatHub(
            IMessageService messageService,
            IChatRoomService chatRoomService,
            IPresenceService presenceService)
        {
            _messageService = messageService;
            _chatRoomService = chatRoomService;
            _presenceService = presenceService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            _presenceService.UserConnected(userId, Context.ConnectionId);

            var rooms = await _chatRoomService.GetRoomsByUser(userId);
            foreach (var room in rooms)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomId.ToString());
            }

            await Clients.All.SendAsync("UserOnline", userId);
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

        public async Task SendDirectMessage(int receiverId, string content)
        {
            var senderId = GetCurrentUserId();
            var message = await _messageService.SendMessage(new MessageEntity
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                MessageType = "TEXT"
            });

            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", message);
            await Clients.Caller.SendAsync("ReceiveMessage", message);
        }

        public async Task SendRoomMessage(int roomId, string content)
        {
            var senderId = GetCurrentUserId();
            var message = await _messageService.SendMessage(new MessageEntity
            {
                SenderId = senderId,
                RoomId = roomId,
                Content = content,
                MessageType = "TEXT"
            });

            await Clients.Group(roomId.ToString()).SendAsync("ReceiveRoomMessage", message);
        }

        public async Task TypingIndicator(int recipientId, bool isTyping)
        {
            var senderId = GetCurrentUserId();
            await Clients.User(recipientId.ToString()).SendAsync("UserTyping", senderId, isTyping);
        }

        public async Task JoinRoom(int roomId)
        {
            var userId = GetCurrentUserId();
            var isMember = await _chatRoomService.IsUserInRoom(roomId, userId);
            if (!isMember)
            {
                await _chatRoomService.AddMember(roomId, userId);
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
        }

        public async Task LeaveRoom(int roomId)
        {
            var userId = GetCurrentUserId();
            await _chatRoomService.LeaveRoom(roomId, userId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
        }

        private int GetCurrentUserId()
        {
            var userIdValue = Context.UserIdentifier;
            if (int.TryParse(userIdValue, out var userId))
            {
                return userId;
            }

            throw new HubException("User is not authenticated.");
        }
    }
}
