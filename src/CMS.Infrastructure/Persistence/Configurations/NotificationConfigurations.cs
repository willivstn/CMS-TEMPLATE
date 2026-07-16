using CMS.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.Link).HasMaxLength(512);
        builder.Property(n => n.Icon).HasMaxLength(64);
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(n => new { n.TenantId, n.CreatedOnUtc });
    }
}

public sealed class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
    {
        builder.ToTable("NotificationRecipients");
        builder.HasIndex(r => new { r.NotificationId, r.UserId }).IsUnique();
        builder.HasIndex(r => new { r.UserId, r.IsRead });

        builder.HasOne(r => r.Notification)
            .WithMany(n => n.Recipients)
            .HasForeignKey(r => r.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
