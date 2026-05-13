using ConnectHub.Notification.Data;
using ConnectHub.Notification.Hubs;
using ConnectHub.Notification.Models;
using ConnectHub.Notification.Repositories;
using ConnectHub.Notification.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using NotificationEntity = ConnectHub.Notification.Models.Notification;

namespace ConnectHub.Notification.Tests
{
    public class NotificationServiceTests
    {
        [Fact]
        public async Task Send_SavesNotificationAndNotifiesHub_WhenRecipientOffline()
        {
            var options = new DbContextOptionsBuilder<NotificationDbContext>()
                .UseInMemoryDatabase(databaseName: "notif_test_db")
                .Options;

            await using var context = new NotificationDbContext(options);
            var repository = new NotificationRepository(context);

            var mockHubContext = new Mock<IHubContext<NotificationHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            mockHubContext.SetupGet(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

            var mockPresence = new Mock<IPresenceService>();
            mockPresence.Setup(p => p.IsUserOnline(It.IsAny<int>())).Returns(false);

            var smtp = Options.Create(new SmtpSettings { Host = "", FromEmail = "" });

            var service = new NotificationService(repository, mockHubContext.Object, mockPresence.Object, smtp);

            var notif = new NotificationEntity { RecipientId = 123, Title = "Hi", Message = "msg", Type = NotificationType.MESSAGE };

            var result = await service.Send(notif);

            Assert.NotNull(result);
            Assert.Equal(123, result.RecipientId);

            var saved = (await repository.FindByRecipientId(123)).FirstOrDefault();
            Assert.NotNull(saved);

            mockClients.Verify(c => c.User("123"), Times.AtLeastOnce);
            mockClientProxy.Verify(p => p.SendCoreAsync("NotificationCount", It.IsAny<object[]>(), default), Times.AtLeastOnce);
        }
    }
}
