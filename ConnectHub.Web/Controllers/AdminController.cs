using Microsoft.AspNetCore.Mvc;
using ConnectHub.Web.Services;

namespace ConnectHub.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IChatRoomService _roomService;
        private readonly IMessageService _messageService;
        private readonly INotificationService _notifService;

        public AdminController(
            IUserService userService,
            IChatRoomService roomService,
            IMessageService messageService,
            INotificationService notifService)
        {
            _userService = userService;
            _roomService = roomService;
            _messageService = messageService;
            _notifService = notifService;
        }

        public IActionResult AdminDashboard() => View();
        public IActionResult ManageAllUsers() => View(_userService.SearchUsers(""));
        public IActionResult SuspendUser(int id) { /* TODO: Suspend logic */ return RedirectToAction("ManageAllUsers"); }
        public IActionResult DeleteUser(int id) { /* TODO: Delete logic */ return RedirectToAction("ManageAllUsers"); }
        public IActionResult ViewAllRooms() => View(_roomService.GetAllRooms());
        public IActionResult DeleteRoom(int id) { /* TODO: Delete logic */ return RedirectToAction("ViewAllRooms"); }
        public IActionResult ViewAllMessages() => View(_messageService.GetMessagesForRoom(0));
        public IActionResult DeleteMessage(int id) { /* TODO: Delete logic */ return RedirectToAction("ViewAllMessages"); }
        public IActionResult ViewPlatformAnalytics() => View();
        public IActionResult SendPlatformNotification(string title, string message) { /* TODO: Notification logic */ return RedirectToAction("AdminDashboard"); }
        public IActionResult ViewAuditLogs() => View();
        public IActionResult ViewActiveConnections() => View();
    }
}
