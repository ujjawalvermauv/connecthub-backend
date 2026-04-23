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
                entity.HasKey(room => room.RoomId);
                entity.Property(room => room.RoomName).HasMaxLength(100).IsRequired();
                entity.Property(room => room.RoomType).HasMaxLength(20).IsRequired();
                entity.Property(room => room.Description).HasMaxLength(500);
                entity.Property(room => room.AvatarUrl).HasMaxLength(500);
                entity.HasMany(room => room.Members)
                    .WithOne(member => member.Room)
                    .HasForeignKey(member => member.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RoomMember>(entity =>
            {
                entity.HasKey(member => member.MemberId);
                entity.Property(member => member.Role).HasMaxLength(20).IsRequired();
                entity.HasIndex(member => new { member.RoomId, member.UserId }).IsUnique();
            });
        }
    }
}
