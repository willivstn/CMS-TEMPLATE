using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CMS.Domain.Authorization;
using CMS.Web.Authorization;
using CMS.Web.Models;

namespace CMS.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Demonstrates permission-based authorization end-to-end: only principals
    /// holding <c>Dashboard.View</c> reach this action; others are redirected to
    /// the access-denied page by the permission policy.
    /// </summary>
    [HasPermission(Permissions.Dashboard.View)]
    public IActionResult Secure()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
