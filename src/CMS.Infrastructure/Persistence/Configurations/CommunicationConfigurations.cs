using CMS.Domain.Entities.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

public sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");
        builder.Property(t => t.Key).HasMaxLength(128).IsRequired();
        builder.Property(t => t.Subject).HasMaxLength(256).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(512);
        builder.Property(t => t.AvailableTokens).HasMaxLength(1024);
        builder.HasIndex(t => new { t.TenantId, t.Key }).IsUnique();
    }
}

public sealed class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("EmailMessages");
        builder.Property(m => m.To).HasMaxLength(1024).IsRequired();
        builder.Property(m => m.Cc).HasMaxLength(1024);
        builder.Property(m => m.Bcc).HasMaxLength(1024);
        builder.Property(m => m.Subject).HasMaxLength(256).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(m => new { m.Status, m.ScheduledOnUtc });

        builder.HasOne(m => m.Template)
            .WithMany()
            .HasForeignKey(m => m.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
