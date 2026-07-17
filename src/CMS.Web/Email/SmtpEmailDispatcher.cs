using System.Net;
using System.Net.Mail;
using CMS.Domain.Entities.Communication;
using Microsoft.Extensions.Options;

namespace CMS.Web.Email;

/// <summary>
/// Sends a single queued email via SMTP. When SMTP is disabled (the default), it
/// logs the message instead so development flows work without a mail server.
/// Drained by the email-queue background job.
/// </summary>
public sealed class SmtpEmailDispatcher(IOptions<SmtpOptions> options, ILogger<SmtpEmailDispatcher> logger)
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("DEV EMAIL (SMTP disabled) → To: {To} | Subject: {Subject}", message.To, message.Subject);
            return;
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true,
        };

        foreach (var recipient in message.To.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            mail.To.Add(recipient);
        }

        using var client = new SmtpClient(_options.Host, _options.Port) { EnableSsl = _options.EnableSsl };
        if (!string.IsNullOrEmpty(_options.User))
        {
            client.Credentials = new NetworkCredential(_options.User, _options.Password);
        }

        await client.SendMailAsync(mail, cancellationToken);
    }
}
