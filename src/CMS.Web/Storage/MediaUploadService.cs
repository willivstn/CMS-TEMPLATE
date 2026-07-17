using System.Security.Cryptography;
using CMS.Application.Common.Abstractions;
using CMS.Application.Features.Media;
using CMS.Domain.Enums;
using CMS.Shared.Results;
using MediatR;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace CMS.Web.Storage;

/// <summary>
/// Orchestrates a file upload: stores the original via <see cref="IFileStorage"/>,
/// generates a thumbnail and reads dimensions for images (ImageSharp), computes a
/// content hash, then persists metadata through the <see cref="CreateMediaItemCommand"/>.
/// Kept in the Web layer because it works with <see cref="IFormFile"/>.
/// </summary>
public sealed class MediaUploadService(IFileStorage storage, ISender mediator, ILogger<MediaUploadService> logger)
{
    private const int ThumbnailMax = 400;

    public async Task<Result<Guid>> UploadAsync(IFormFile file, Guid? folderId, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            return Result.Failure<Guid>(Error.Validation("Media.Empty", $"'{file.FileName}' is empty."));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var type = Classify(file.ContentType, extension);

        // Buffer once so we can hash, measure and store without re-reading the request stream.
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var checksum = Convert.ToHexString(SHA256.HashData(bytes));

        int? width = null, height = null;
        string? thumbnailUrl = null;

        buffer.Position = 0;
        var stored = await storage.SaveAsync(buffer, storedName, file.ContentType, cancellationToken);

        if (type == MediaType.Image)
        {
            (width, height, thumbnailUrl) = await TryProcessImageAsync(bytes, storedName, cancellationToken);
        }

        return await mediator.Send(
            new CreateMediaItemCommand(
                folderId, storedName, file.FileName, file.ContentType, extension, file.Length, type,
                storage.ProviderName, stored.StorageKey, stored.Url, width, height, thumbnailUrl, checksum),
            cancellationToken);
    }

    private async Task<(int? Width, int? Height, string? ThumbnailUrl)> TryProcessImageAsync(
        byte[] bytes, string storedName, CancellationToken cancellationToken)
    {
        try
        {
            using var image = Image.Load(bytes);
            var width = image.Width;
            var height = image.Height;

            image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(ThumbnailMax, ThumbnailMax) }));

            using var thumbStream = new MemoryStream();
            await image.SaveAsJpegAsync(thumbStream, cancellationToken);
            thumbStream.Position = 0;

            var thumbName = $"{Path.GetFileNameWithoutExtension(storedName)}_thumb.jpg";
            var thumb = await storage.SaveAsync(thumbStream, thumbName, "image/jpeg", cancellationToken);
            return (width, height, thumb.Url);
        }
        catch (Exception ex)
        {
            // Not a decodable image; store it without a thumbnail.
            logger.LogWarning(ex, "Thumbnail generation failed for {File}", storedName);
            return (null, null, null);
        }
    }

    private static MediaType Classify(string contentType, string extension)
    {
        contentType = contentType.ToLowerInvariant();

        if (contentType.StartsWith("image/", StringComparison.Ordinal))
        {
            return MediaType.Image;
        }

        if (contentType.StartsWith("video/", StringComparison.Ordinal))
        {
            return MediaType.Video;
        }

        if (contentType.StartsWith("audio/", StringComparison.Ordinal))
        {
            return MediaType.Audio;
        }

        if (contentType == "application/pdf" || extension == ".pdf")
        {
            return MediaType.Pdf;
        }

        string[] documentExtensions = [".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".rtf", ".odt"];
        return documentExtensions.Contains(extension) ? MediaType.Document : MediaType.Other;
    }
}
