using MediatR;

namespace CMS.Domain.Common;

/// <summary>
/// Marker for something that happened in the domain worth reacting to.
/// Domain events are raised by aggregates and dispatched after the owning
/// transaction commits, keeping side effects decoupled from the entity itself.
/// Implements <see cref="INotification"/> so handlers are resolved via MediatR.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>When the event occurred (UTC).</summary>
    DateTimeOffset OccurredOnUtc { get; }
}
