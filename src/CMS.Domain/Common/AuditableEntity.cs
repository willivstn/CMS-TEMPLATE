namespace CMS.Domain.Common;

/// <summary>
/// The default base for domain entities: a <see cref="Guid"/> primary key with
/// full audit trail and soft-delete support. Most CMS entities derive from this.
/// Audit and soft-delete fields are set automatically by SaveChanges interceptors.
/// </summary>
public abstract class AuditableEntity : Entity<Guid>, IAuditable, ISoftDeletable
{
    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; set; }

    /// <inheritdoc />
    public string? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; set; }

    /// <inheritdoc />
    public string? ModifiedBy { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedOnUtc { get; set; }

    /// <inheritdoc />
    public string? DeletedBy { get; set; }
}
