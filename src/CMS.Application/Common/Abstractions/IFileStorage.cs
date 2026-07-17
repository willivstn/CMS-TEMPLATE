namespace CMS.Application.Common.Abstractions;

/// <summary>The result of storing a file: its provider key and a resolvable URL.</summary>
public sealed record StoredFile(string StorageKey, string Url);

/// <summary>
/// Pluggable blob storage. The default implementation writes to the local file
/// system; Azure Blob / S3 providers implement the same contract. Keys are opaque
/// to callers and used later to delete or (re)resolve the file.
/// </summary>
public interface IFileStorage
{
    /// <summary>Persists <paramref name="content"/> under a generated key and returns its public URL.</summary>
    Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Deletes a stored blob by key. No-op if it does not exist.</summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>The provider identifier persisted with each asset (e.g. "Local").</summary>
    string ProviderName { get; }
}
