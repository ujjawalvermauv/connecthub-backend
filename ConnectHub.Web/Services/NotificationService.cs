using System.Collections.Generic;

namespace ConnectHub.Web.Services
{
    public interface INotificationService
    {
        IEnumerable<string> GetNotificationsForUser(int userId);
        void SendNotification(int userId, string message);
    }

    public class NotificationService : INotificationService
    {
        public IEnumerable<string> GetNotificationsForUser(int userId)
        {
            // TODO: Implement DB logic
            return new List<string>();
        }
        public void SendNotification(int userId, string message)
        {
            // TODO: Implement send logic
        }
    }
}
