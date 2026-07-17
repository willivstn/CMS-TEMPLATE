using CMS.Application.Features.Theming;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.ViewComponents;

/// <summary>Renders the topbar dropdown for choosing a custom theme.</summary>
public sealed class ThemePickerViewComponent(ISender mediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
        => View(await mediator.Send(new GetEnabledThemesQuery()));
}
