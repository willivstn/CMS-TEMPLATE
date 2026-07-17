namespace CMS.Web.Email;

/// <summary>Binds the "Email:Smtp" configuration section.</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Email:Smtp";

    /// <summary>When false (default), emails are logged instead of sent — useful in development.</summary>
    public bool Enabled { get; set; }

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "no-reply@cms.local";
    public string FromName { get; set; } = "CMS";
}
