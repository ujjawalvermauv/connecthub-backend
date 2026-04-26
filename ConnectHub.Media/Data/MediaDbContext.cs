using ConnectHub.Media.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.Media.Data
{
    public class MediaDbContext : DbContext
    {
        public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options)
        {
        }

        public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MediaFile>(entity =>
            {
                entity.HasKey(mediaFile => mediaFile.FileId);

                entity.Property(mediaFile => mediaFile.FileId)
                    .HasMaxLength(32)
                    .IsRequired();

                entity.Property(mediaFile => mediaFile.FileName)
                    .HasMaxLength(260)
                    .IsRequired();

                entity.Property(mediaFile => mediaFile.ContentType)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(mediaFile => mediaFile.BlobUrl)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(mediaFile => mediaFile.ThumbnailUrl)
                    .HasMaxLength(1000);

                entity.HasIndex(mediaFile => mediaFile.UploadedBy);
                entity.HasIndex(mediaFile => mediaFile.MessageId);
                entity.HasIndex(mediaFile => mediaFile.RoomId);
                entity.HasIndex(mediaFile => mediaFile.ExpiresAt);
                entity.HasIndex(mediaFile => mediaFile.UploadedAt);
            });
        }
    }
}
