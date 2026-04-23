using ConnectHub.ChatHub.Services;
using Microsoft.AspNetCore.SignalR;

namespace ConnectHub.ChatHub.Hubs
{
	public class ChatHub : Hub
	{
		private readonly IChatRoomService _chatRoomService;

		public ChatHub(IChatRoomService chatRoomService)
		{
			_chatRoomService = chatRoomService;
		}

		public async Task JoinRoom(int roomId, int userId)
		{
			await _chatRoomService.AddMember(roomId, userId);
			await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
		}

		public async Task LeaveRoom(int roomId, int userId)
		{
			await _chatRoomService.LeaveRoom(roomId, userId);
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
		}

		public async Task SendRoomMessage(int roomId, int senderId, string message)
		{
			await Clients.Group(roomId.ToString())
				.SendAsync("ReceiveRoomMessage", new
				{
					RoomId = roomId,
					SenderId = senderId,
					Content = message,
					SentAt = DateTime.UtcNow
				});
		}
	}
}
