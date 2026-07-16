using CMS.Domain.Entities.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

public sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("Languages");
        builder.Property(l => l.Code).HasMaxLength(16).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(64).IsRequired();
        builder.Property(l => l.NativeName).HasMaxLength(64);
        builder.Property(l => l.FlagIcon).HasMaxLength(64);
        builder.Property(l => l.CreatedBy).HasMaxLength(256);
        builder.Property(l => l.ModifiedBy).HasMaxLength(256);
        builder.Property(l => l.DeletedBy).HasMaxLength(256);
        builder.HasIndex(l => l.Code).IsUnique();
    }
}

public sealed class TranslationConfiguration : IEntityTypeConfiguration<Translation>
{
    public void Configure(EntityTypeBuilder<Translation> builder)
    {
        builder.ToTable("Translations");
        builder.Property(t => t.Key).HasMaxLength(256).IsRequired();
        builder.Property(t => t.LanguageCode).HasMaxLength(16).IsRequired();
        builder.Property(t => t.Value).HasMaxLength(2048).IsRequired();
        builder.Property(t => t.Group).HasMaxLength(128);
        builder.HasIndex(t => new { t.Key, t.LanguageCode }).IsUnique();

        builder.HasOne(t => t.Language)
            .WithMany(l => l.Translations)
            .HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
