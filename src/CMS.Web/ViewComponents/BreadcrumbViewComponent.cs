using CMS.Web.Navigation;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.ViewComponents;

/// <summary>Renders breadcrumbs derived from the active branch of the navigation.</summary>
public sealed class BreadcrumbViewComponent(INavigationService navigation) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var trail = await navigation.GetBreadcrumbsAsync();
        return View(trail);
    }
}
