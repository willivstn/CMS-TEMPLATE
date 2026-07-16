namespace CMS.Web.Models.Components;

/// <summary>View data for the reusable <c>_Pager.cshtml</c> component.</summary>
public sealed record PagerModel(int PageNumber, int TotalPages, int TotalCount, int FirstItemIndex, int LastItemIndex);
