using CMS.Domain.Common;
using CMS.Domain.Enums;

namespace CMS.Domain.Entities.Media;

/// <summary>
/// A stored file/media asset — the single model behind both the "Files" and
/// "Media Library" concepts. Backed by a pluggable storage provider (local,
/// Azure Blob, S3) addressed by <see cref="StorageKey"/>. Supports thumbnails,
/// metadata, content-hash de-duplication, versioning and soft delete/restore.
/// </summary>
public class MediaItem : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid? FolderId { get; set; }
    public MediaFolder? Folder { get; set; }

    /// <summary>Sanitised, unique stored file name.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>The file name as originally uploaded.</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>MIME type, e.g. "image/png".</summary>
    public string ContentType { get; set; } = "application/octet-stream";

    public string? Extension { get; set; }

    public long SizeBytes { get; set; }

    public MediaType Type { get; set; } = MediaType.Other;

    /// <summary>Storage provider key, e.g. "Local", "AzureBlob", "S3".</summary>
    public string StorageProvider { get; set; } = "Local";

    /// <summary>Provider-specific key/path used to retrieve the blob.</summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>Publicly resolvable URL (may be CDN-fronted).</summary>
    public string? Url { get; set; }

    // Image-specific metadata
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? ThumbnailUrl { get; set; }

    // Descriptive metadata
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public string? Caption { get; set; }

    /// <summary>SHA-256 content hash for de-duplication and integrity.</summary>
    public string? Checksum { get; set; }

    /// <summary>Incremented when the underlying file is replaced.</summary>
    public int Version { get; set; } = 1;
}
