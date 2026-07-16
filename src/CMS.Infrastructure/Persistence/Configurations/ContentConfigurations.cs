using CMS.Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.Property(c => c.Name).HasMaxLength(128).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(160).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(512);
        builder.HasIndex(c => new { c.TenantId, c.Slug }).IsUnique();

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.Property(t => t.Name).HasMaxLength(96).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(120).IsRequired();
        builder.HasIndex(t => new { t.TenantId, t.Slug }).IsUnique();
    }
}

public sealed class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.ToTable("ContentItems");

        builder.Property(c => c.Title).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Summary).HasMaxLength(1024);
        builder.Property(c => c.MetaTitle).HasMaxLength(256);
        builder.Property(c => c.MetaDescription).HasMaxLength(512);
        builder.Property(c => c.MetaKeywords).HasMaxLength(256);
        builder.Property(c => c.CanonicalUrl).HasMaxLength(512);
        builder.Property(c => c.OpenGraphImageUrl).HasMaxLength(512);

        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(c => new { c.TenantId, c.Slug }).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.Type, c.Status });
        builder.HasIndex(c => c.PublishedOnUtc);

        builder.HasOne(c => c.Category)
            .WithMany(cat => cat.ContentItems)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.FeaturedMedia)
            .WithMany()
            .HasForeignKey(c => c.FeaturedMediaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Versions)
            .WithOne(v => v.ContentItem)
            .HasForeignKey(v => v.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContentVersionConfiguration : IEntityTypeConfiguration<ContentVersion>
{
    public void Configure(EntityTypeBuilder<ContentVersion> builder)
    {
        builder.ToTable("ContentVersions");
        builder.Property(v => v.Title).HasMaxLength(256).IsRequired();
        builder.Property(v => v.Summary).HasMaxLength(1024);
        builder.Property(v => v.Note).HasMaxLength(512);
        builder.Property(v => v.CreatedBy).HasMaxLength(256);
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(v => new { v.ContentItemId, v.VersionNumber }).IsUnique();
    }
}

public sealed class ContentTagConfiguration : IEntityTypeConfiguration<ContentTag>
{
    public void Configure(EntityTypeBuilder<ContentTag> builder)
    {
        builder.ToTable("ContentTags");
        builder.HasIndex(ct => new { ct.ContentItemId, ct.TagId }).IsUnique();

        builder.HasOne(ct => ct.ContentItem)
            .WithMany(c => c.ContentTags)
            .HasForeignKey(ct => ct.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ct => ct.Tag)
            .WithMany(t => t.ContentTags)
            .HasForeignKey(ct => ct.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
