using System.Text.Json.Serialization;

namespace ConnectHub.Notification.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NotificationType
    {
        MESSAGE,
        MENTION,
        ROOM_INVITE,
        ROLE_CHANGE,
        PLATFORM
    }
}