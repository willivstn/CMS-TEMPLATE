using CMS.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace CMS.Infrastructure.Email;

/// <summary>
/// Development email sender that writes messages to the log instead of sending
/// them, so account flows (confirmation, password reset) are exercisable without
/// an SMTP server. Replaced by a real provider + queue in Phase 6.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "DEV EMAIL → To: {To} | Subject: {Subject}\n{Body}",
            to, subject, htmlBody);

        return Task.CompletedTask;
    }
}
