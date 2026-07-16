namespace CMS.Domain.Common;

/// <summary>Base marker for all persistent entities.</summary>
public interface IEntity;

/// <summary>An entity with a strongly-typed primary key.</summary>
/// <typeparam name="TKey">The primary-key type.</typeparam>
public interface IEntity<out TKey> : IEntity
{
    /// <summary>The primary key.</summary>
    TKey Id { get; }
}

/// <summary>
/// An entity that buffers domain events for post-commit dispatch. Lets the
/// persistence layer discover and publish events without knowing concrete types.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>Events raised by this entity awaiting dispatch.</summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>Clears buffered events after they have been dispatched.</summary>
    void ClearDomainEvents();
}

/// <summary>
/// Records who created and last modified an entity and when. Populated
/// automatically by the auditing <c>SaveChanges</c> interceptor.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedOnUtc { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? ModifiedOnUtc { get; set; }
    string? ModifiedBy { get; set; }
}

/// <summary>
/// Marks an entity as soft-deletable. A global query filter hides deleted rows;
/// the soft-delete interceptor converts hard deletes into flag updates so data
/// can be restored and audited.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedOnUtc { get; set; }
    string? DeletedBy { get; set; }
}

/// <summary>
/// Marks an entity as belonging to a tenant. A global query filter scopes
/// queries to the current tenant. The template runs single-tenant by default
/// but the plumbing is present so multi-tenancy can be switched on without
/// touching entities or queries.
/// </summary>
public interface ITenantEntity
{
    /// <summary>The owning tenant identifier.</summary>
    Guid TenantId { get; set; }
}
