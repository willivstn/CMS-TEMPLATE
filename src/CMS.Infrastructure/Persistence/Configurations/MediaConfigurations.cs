using CMS.Domain.Entities.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

public sealed class MediaFolderConfiguration : IEntityTypeConfiguration<MediaFolder>
{
    public void Configure(EntityTypeBuilder<MediaFolder> builder)
    {
        builder.ToTable("MediaFolders");
        builder.Property(f => f.Name).HasMaxLength(128).IsRequired();
        builder.Property(f => f.Path).HasMaxLength(1024).IsRequired();
        builder.HasIndex(f => new { f.TenantId, f.Path });

        builder.HasOne(f => f.Parent)
            .WithMany(f => f.Children)
            .HasForeignKey(f => f.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MediaItemConfiguration : IEntityTypeConfiguration<MediaItem>
{
    public void Configure(EntityTypeBuilder<MediaItem> builder)
    {
        builder.ToTable("MediaItems");

        builder.Property(m => m.FileName).HasMaxLength(256).IsRequired();
        builder.Property(m => m.OriginalFileName).HasMaxLength(256).IsRequired();
        builder.Property(m => m.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(m => m.Extension).HasMaxLength(16);
        builder.Property(m => m.StorageProvider).HasMaxLength(32).IsRequired();
        builder.Property(m => m.StorageKey).HasMaxLength(1024).IsRequired();
        builder.Property(m => m.Url).HasMaxLength(1024);
        builder.Property(m => m.ThumbnailUrl).HasMaxLength(1024);
        builder.Property(m => m.AltText).HasMaxLength(256);
        builder.Property(m => m.Title).HasMaxLength(256);
        builder.Property(m => m.Caption).HasMaxLength(512);
        builder.Property(m => m.Checksum).HasMaxLength(128);
        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(m => new { m.TenantId, m.FolderId });
        builder.HasIndex(m => m.Checksum);
        builder.HasIndex(m => m.IsDeleted);

        builder.HasOne(m => m.Folder)
            .WithMany(f => f.Items)
            .HasForeignKey(m => m.FolderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
