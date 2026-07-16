using CMS.Domain.Common;

namespace CMS.Domain.Entities.System;

/// <summary>
/// A structured application log entry persisted to the database (Serilog sink
/// target). Complements rolling file logs and enables in-app log browsing.
/// </summary>
public class SystemLog : Entity<Guid>
{
    /// <summary>Serilog level, e.g. "Information", "Warning", "Error".</summary>
    public string Level { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>The message template before property substitution.</summary>
    public string? MessageTemplate { get; set; }

    public string? Exception { get; set; }

    /// <summary>Structured properties serialized as JSON.</summary>
    public string? Properties { get; set; }

    public string? SourceContext { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestId { get; set; }
    public Guid? UserId { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }
}
