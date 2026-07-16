namespace CMS.Domain.Common;

/// <summary>
/// Base class for entities with a strongly-typed key. Provides domain-event
/// buffering and value-style equality based on identity.
/// </summary>
/// <typeparam name="TKey">The primary-key type.</typeparam>
public abstract class Entity<TKey> : IEntity<TKey>, IHasDomainEvents, IEquatable<Entity<TKey>>
    where TKey : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>The primary key.</summary>
    public TKey Id { get; set; } = default!;

    /// <summary>Domain events raised by this entity, awaiting dispatch after commit.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Buffers a domain event to be dispatched when changes are saved.</summary>
    public void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Clears buffered domain events (called by the dispatcher after commit).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool Equals(Entity<TKey>? other)
    {
        if (other is null || other.GetType() != GetType())
        {
            return false;
        }

        return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TKey> entity && Equals(entity);

    public override int GetHashCode() => EqualityComparer<TKey>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right) => Equals(left, right);

    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right) => !Equals(left, right);
}
