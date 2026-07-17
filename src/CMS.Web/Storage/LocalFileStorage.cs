using CMS.Application.Common.Abstractions;

namespace CMS.Web.Storage;

/// <summary>
/// Stores uploads on the local file system under <c>wwwroot/uploads/yyyy/MM</c> and
/// serves them as static files. The default <see cref="IFileStorage"/> provider;
/// swap for an Azure Blob / S3 implementation (in Infrastructure) via DI to move
/// to cloud storage without touching callers.
/// </summary>
public sealed class LocalFileStorage(IWebHostEnvironment environment) : IFileStorage
{
    private const string PublicRoot = "uploads";

    public string ProviderName => "Local";

    private string PhysicalRoot =>
        Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), PublicRoot);

    public async Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var relativeDir = DateTime.UtcNow.ToString("yyyy/MM", System.Globalization.CultureInfo.InvariantCulture);
        var absoluteDir = Path.Combine(PhysicalRoot, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var safeName = Path.GetFileName(fileName);
        var absolutePath = Path.Combine(absoluteDir, safeName);

        await using (var target = File.Create(absolutePath))
        {
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            await content.CopyToAsync(target, cancellationToken);
        }

        var key = $"{relativeDir}/{safeName}";
        var url = $"/{PublicRoot}/{key}";
        return new StoredFile(key, url);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var absolutePath = Path.Combine(PhysicalRoot, storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }
}
