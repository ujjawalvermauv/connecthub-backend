using ConnectHub.Message.Models;
using MessageEntity = ConnectHub.Message.Models.Message;

namespace ConnectHub.Message.Data
{
    public static class MessageSeedData
    {
        public static readonly MessageEntity[] SeedMessages =
        [
            new MessageEntity
            {
                MessageId = 1,
                SenderId = 2222,
                ReceiverId = 4444,
                Content = "Hey, are you free for a quick call?",
                MessageType = "TEXT",
                IsRead = false,
                IsDeleted = false,
                IsEdited = false,
                SentAt = DateTime.UtcNow.AddMinutes(-30)
            },
            new MessageEntity
            {
                MessageId = 2,
                SenderId = 4444,
                ReceiverId = 2222,
                Content = "Yes, give me five minutes.",
                MessageType = "TEXT",
                IsRead = true,
                IsDeleted = false,
                IsEdited = false,
                SentAt = DateTime.UtcNow.AddMinutes(-25),
                ReadAt = DateTime.UtcNow.AddMinutes(-24)
            },
            new MessageEntity
            {
                MessageId = 3,
                SenderId = 2222,
                RoomId = 101,
                Content = "Welcome to the product planning room.",
                MessageType = "TEXT",
                IsRead = true,
                IsDeleted = false,
                IsEdited = false,
                SentAt = DateTime.UtcNow.AddHours(-2),
                ReadAt = DateTime.UtcNow.AddHours(-2).AddMinutes(1)
            },
            new MessageEntity
            {
                MessageId = 4,
                SenderId = 3333,
                RoomId = 101,
                Content = "I uploaded the latest mockups.",
                MessageType = "FILE",
                IsRead = false,
                IsDeleted = false,
                IsEdited = false,
                SentAt = DateTime.UtcNow.AddMinutes(-12),
                MediaUrl = "https://example.com/mockups.pdf"
            },
            new MessageEntity
            {
                MessageId = 5,
                SenderId = 2222,
                ReceiverId = 5555,
                Content = "Let me know if the API changes are ready.",
                MessageType = "TEXT",
                IsRead = false,
                IsDeleted = false,
                IsEdited = true,
                SentAt = DateTime.UtcNow.AddMinutes(-8),
                EditedAt = DateTime.UtcNow.AddMinutes(-7)
            }
        ];
    }
}
