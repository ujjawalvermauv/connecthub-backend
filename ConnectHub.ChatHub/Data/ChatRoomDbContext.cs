using ConnectHub.ChatHub.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.ChatHub.Data
{
    public class ChatRoomDbContext : DbContext
    {
        public ChatRoomDbContext(DbContextOptions<ChatRoomDbContext> options) : base(options)
        {
        }

        public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
        public DbSet<RoomMember> RoomMembers => Set<RoomMember>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChatRoom>(entity =>
            {
                entity.HasKey(r => r.RoomId);

                entity.Property(r => r.RoomName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(r => r.RoomType)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.HasMany(r => r.Members)
                    .WithOne(m => m.Room)
                    .HasForeignKey(m => m.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RoomMember>(entity =>
            {
                entity.HasKey(m => m.MemberId);

                entity.Property(m => m.Role)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.HasIndex(m => new { m.RoomId, m.UserId })
                    .IsUnique();
            });
        }
    }
}
