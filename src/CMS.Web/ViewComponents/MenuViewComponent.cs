using CMS.Web.Navigation;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.ViewComponents;

/// <summary>Renders the permission-filtered sidebar navigation from the database menu.</summary>
public sealed class MenuViewComponent(INavigationService navigation) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var roots = await navigation.GetMenuAsync();
        return View(roots);
    }
}
