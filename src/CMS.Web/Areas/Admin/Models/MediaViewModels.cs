using CMS.Application.Features.Media;
using CMS.Shared.Models;

namespace CMS.Web.Areas.Admin.Models;

/// <summary>The media library grid: current folder's subfolders and files.</summary>
public sealed class MediaLibraryViewModel
{
    public Guid? CurrentFolderId { get; set; }
    public string? Search { get; set; }
    public IReadOnlyList<MediaFolderDto> Folders { get; set; } = [];
    public PagedResult<MediaItemDto> Items { get; set; } = new([], 0, 1, 24);
}
