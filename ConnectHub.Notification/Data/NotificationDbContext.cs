using ConnectHub.Notification.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NotificationEntity = ConnectHub.Notification.Models.Notification;

namespace ConnectHub.Notification.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        {
        }

        public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NotificationEntity>(entity =>
            {
                entity.HasKey(notification => notification.NotificationId);

                entity.Property(notification => notification.Type)
                    .HasConversion(new EnumToStringConverter<NotificationType>())
                    .HasMaxLength(30)
                    .IsRequired();

                entity.HasIndex(notification => new { notification.RecipientId, notification.IsRead, notification.SentAt });
                entity.HasIndex(notification => new { notification.RelatedId, notification.RelatedType });
                entity.HasIndex(notification => notification.Type);

                entity.Property(notification => notification.Title)
                    .HasMaxLength(250)
                    .IsRequired();

                entity.Property(notification => notification.Message)
                    .HasMaxLength(4000)
                    .IsRequired();

                entity.Property(notification => notification.RelatedType)
                    .HasMaxLength(100);
            });
        }
    }
}