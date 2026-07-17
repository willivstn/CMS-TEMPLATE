using CMS.Domain.Entities.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

/// <summary>Mapping for the sample Announcements module entity.</summary>
public sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("Announcements");
        builder.Property(a => a.Title).HasMaxLength(256).IsRequired();
        builder.Property(a => a.CreatedBy).HasMaxLength(256);
        builder.Property(a => a.ModifiedBy).HasMaxLength(256);
        builder.Property(a => a.DeletedBy).HasMaxLength(256);
        builder.HasIndex(a => new { a.TenantId, a.IsPublished });
    }
}
