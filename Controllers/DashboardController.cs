using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CipherVault.Controllers;

/// <summary>
/// Placeholder dashboard for Phase 1. Phase 3 will inject IVaultItemService
/// to populate item counts, category breakdown, and recent items.
/// </summary>
[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Dashboard";
        return View();
    }
}
