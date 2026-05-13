using ConnectHub.ChatHub.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.ChatHub.Data
{
    public class ChatHubDbContext : DbContext
    {
        public ChatHubDbContext(DbContextOptions<ChatHubDbContext> options) : base(options) { }

        public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
        public DbSet<RoomMember> RoomMembers => Set<RoomMember>();
        public DbSet<Message> Messages => Set<Message>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatRoom>(e =>
            {
                e.HasKey(r => r.RoomId);
                e.Property(r => r.RoomName).IsRequired().HasMaxLength(100);
                e.Property(r => r.RoomType).IsRequired().HasMaxLength(20);
                e.HasIndex(r => r.RoomType);
                e.HasIndex(r => r.CreatedBy);
            });

            modelBuilder.Entity<RoomMember>(e =>
            {
                e.HasKey(m => m.MemberId);
                e.HasIndex(m => new { m.RoomId, m.UserId }).IsUnique();
                e.HasIndex(m => m.UserId);
                e.Property(m => m.Role).IsRequired().HasMaxLength(20);
                e.HasOne(m => m.Room)
                 .WithMany(r => r.Members)
                 .HasForeignKey(m => m.RoomId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Message>(e =>
            {
                e.HasKey(m => m.MessageId);
                e.Property(m => m.Content).IsRequired();
                e.Property(m => m.MessageType).IsRequired().HasMaxLength(50);
                e.HasIndex(m => new { m.SenderId, m.ReceiverId });
                e.HasIndex(m => new { m.RoomId, m.CreatedAt });
            });

            modelBuilder.Entity<ChatRoom>().HasData(
                new ChatRoom { RoomId = 1, RoomName = "General", Description = "General discussion", RoomType = "PUBLIC", CreatedBy = 1, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), MaxMembers = 500, IsActive = true },
                new ChatRoom { RoomId = 2, RoomName = "Dev Team", Description = "Technical discussions", RoomType = "PRIVATE", CreatedBy = 1, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), MaxMembers = 50, IsActive = true },
                new ChatRoom { RoomId = 3, RoomName = "Random", Description = "Anything goes", RoomType = "PUBLIC", CreatedBy = 1, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), MaxMembers = 500, IsActive = true }
            );
        }
    }
}
