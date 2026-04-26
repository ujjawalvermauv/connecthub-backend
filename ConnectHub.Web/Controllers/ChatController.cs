using Microsoft.AspNetCore.Mvc;
using ConnectHub.Web.Services;
using ConnectHub.Web.Models;

namespace ConnectHub.Web.Controllers
{
    public class ChatController : Controller
    {
        private readonly IUserService _userService;
        private readonly IMessageService _messageService;
        private readonly IChatRoomService _roomService;
        private readonly INotificationService _notifService;
        private readonly IMediaService _mediaService;
        private readonly IPresenceService _presenceService;

        public ChatController(
            IUserService userService,
            IMessageService messageService,
            IChatRoomService roomService,
            INotificationService notifService,
            IMediaService mediaService,
            IPresenceService presenceService)
        {
            _userService = userService;
            _messageService = messageService;
            _roomService = roomService;
            _notifService = notifService;
            _mediaService = mediaService;
            _presenceService = presenceService;
        }

        public IActionResult Home() => View();
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(ViewModels.RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var user = new Models.User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = model.Password // TODO: Hash password
            };
            _userService.Register(user);
            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(ViewModels.LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            // TODO: Implement login logic (validate user)
            return RedirectToAction("ViewDashboard");
        }
        public IActionResult Logout() => View();
        public IActionResult ViewDashboard() => View();
        public IActionResult ViewProfile() => View();
        [HttpGet]
        public IActionResult EditProfile()
        {
            // TODO: Get current user
            var user = _userService.GetUserById(1); // Demo
            var model = new ViewModels.EditProfileViewModel
            {
                DisplayName = user.DisplayName,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult EditProfile(ViewModels.EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            // TODO: Get current user id
            var user = new Models.User
            {
                DisplayName = model.DisplayName,
                Bio = model.Bio,
                AvatarUrl = model.AvatarUrl
            };
            _userService.EditProfile(1, user); // Demo
            return RedirectToAction("ViewProfile");
        }
        public IActionResult ViewDirectMessages(int id) => View();
        public IActionResult ViewRoomChat(int id) => View();
        public IActionResult ViewRooms() => View();
        public IActionResult CreateRoom(ChatRoom room) => View();
        public IActionResult JoinRoom(int id) => View();
        public IActionResult LeaveRoom(int id) => View();
        public IActionResult ViewNotifications() => View();
        public IActionResult SearchUsers(string query)
        {
            var users = _userService.SearchUsers(query);
            return View(users);
        }
        public IActionResult SearchMessages(string query) => View();
        public IActionResult UploadMedia(IFormFile file) => View();
        public IActionResult ViewOnlineUsers() => View();
        public IActionResult ViewMessageHistory(int id) => View();
    }
}
