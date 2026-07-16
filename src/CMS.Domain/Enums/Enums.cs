namespace CMS.Domain.Enums;

/// <summary>The kind of content item — drives routing and templating.</summary>
public enum ContentType
{
    Page = 0,
    Post = 1,
}

/// <summary>Editorial lifecycle state of a content item.</summary>
public enum ContentStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
    Scheduled = 3,
}

/// <summary>Broad classification of a stored media/file asset.</summary>
public enum MediaType
{
    Image = 0,
    Document = 1,
    Video = 2,
    Pdf = 3,
    Audio = 4,
    Other = 5,
}

/// <summary>The CLR/storage shape of a setting value.</summary>
public enum SettingType
{
    String = 0,
    Integer = 1,
    Boolean = 2,
    Json = 3,
    Encrypted = 4,
}

/// <summary>Severity/category of a user-facing notification.</summary>
public enum NotificationType
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
}

/// <summary>The action recorded by an audit-trail entry.</summary>
public enum AuditAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    Login = 3,
    Logout = 4,
    Access = 5,
    PermissionChange = 6,
    RoleChange = 7,
    SettingChange = 8,
}

/// <summary>Execution state of a background job run.</summary>
public enum JobStatus
{
    Queued = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4,
}

/// <summary>Delivery state of a queued email message.</summary>
public enum EmailStatus
{
    Queued = 0,
    Sending = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4,
}
