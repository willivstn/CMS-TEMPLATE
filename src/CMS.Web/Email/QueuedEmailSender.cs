using CMS.Application.Common.Abstractions;
using CMS.Domain.Entities.Communication;
using CMS.Domain.Enums;

namespace CMS.Web.Email;

/// <summary>
/// <see cref="IEmailSender"/> that enqueues messages to the database rather than
/// sending inline. A background job drains the queue via <see cref="SmtpEmailDispatcher"/>,
/// so requests never block on SMTP and transient failures are retried. Supersedes
/// the Infrastructure logging sender.
/// </summary>
public sealed class QueuedEmailSender(IApplicationDbContext db) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        db.EmailMessages.Add(new EmailMessage
        {
            Id = Guid.NewGuid(),
            To = to,
            Subject = subject,
            HtmlBody = htmlBody,
            Status = EmailStatus.Queued,
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
