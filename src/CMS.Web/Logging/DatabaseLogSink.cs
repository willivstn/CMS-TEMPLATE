using System.Text.Json;
using CMS.Domain.Entities.System;
using CMS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace CMS.Web.Logging;

/// <summary>
/// A Serilog sink that persists warning-and-above events to the
/// <c>SystemLogs</c> table via a scoped <see cref="AppDbContext"/>, powering the
/// in-app log viewer. Restricted to Warning+ so routine Information logs (and EF's
/// own command logging) don't recurse or flood the database — those still go to the
/// console and rolling file sinks.
/// </summary>
public sealed class DatabaseLogSink(IServiceScopeFactory scopeFactory) : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Warning)
        {
            return;
        }

        // Never persist EF Core's own events: writing this sink's row goes through
        // EF, which would re-emit EF logs and recurse. Those still reach console/file.
        var source = ReadProperty(logEvent, "SourceContext");
        if (source is not null && source.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.SystemLogs.Add(new SystemLog
            {
                Id = Guid.NewGuid(),
                Level = logEvent.Level.ToString(),
                Message = logEvent.RenderMessage(System.Globalization.CultureInfo.InvariantCulture),
                MessageTemplate = logEvent.MessageTemplate.Text,
                Exception = logEvent.Exception?.ToString(),
                Properties = SerializeProperties(logEvent),
                SourceContext = ReadProperty(logEvent, "SourceContext"),
                RequestPath = ReadProperty(logEvent, "RequestPath"),
                RequestId = ReadProperty(logEvent, "RequestId"),
                TimestampUtc = logEvent.Timestamp,
            });

            db.SaveChanges();
        }
        catch
        {
            // Logging must never throw. A failed DB write is silently dropped
            // (the event is still on the console/file sinks).
        }
    }

    private static string? ReadProperty(LogEvent logEvent, string name) =>
        logEvent.Properties.TryGetValue(name, out var value) ? value.ToString().Trim('"') : null;

    private static string? SerializeProperties(LogEvent logEvent)
    {
        if (logEvent.Properties.Count == 0)
        {
            return null;
        }

        var map = logEvent.Properties.ToDictionary(p => p.Key, p => p.Value.ToString());
        return JsonSerializer.Serialize(map);
    }
}
