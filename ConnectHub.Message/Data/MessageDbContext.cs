using ConnectHub.Message.Models;
using Microsoft.EntityFrameworkCore;
using MessageEntity = ConnectHub.Message.Models.Message;

namespace ConnectHub.Message.Data
{
    public class MessageDbContext : DbContext
    {
        public MessageDbContext(DbContextOptions<MessageDbContext> options) : base(options)
        {
        }

        public DbSet<MessageEntity> Messages => Set<MessageEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MessageEntity>(entity =>
            {
                entity.HasKey(m => m.MessageId);

                entity.HasIndex(m => new { m.SenderId, m.ReceiverId });
                entity.HasIndex(m => new { m.RoomId, m.SentAt });

                entity.Property(m => m.Content)
                    .HasMaxLength(4000)
                    .IsRequired();

                entity.Property(m => m.MessageType)
                    .HasMaxLength(20)
                    .IsRequired();
            });
        }
    }
}
