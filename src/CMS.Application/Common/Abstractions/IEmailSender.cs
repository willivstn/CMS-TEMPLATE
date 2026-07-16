namespace CMS.Application.Common.Abstractions;

/// <summary>
/// Transport-agnostic email sender. Phase 2 ships a dev implementation that logs
/// messages (and confirmation/reset links) so account flows work without SMTP;
/// Phase 6 adds SMTP, templating and a persistent queue behind the same contract.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends (or enqueues) an HTML email.</summary>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
