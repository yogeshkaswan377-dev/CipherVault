using CipherVault.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CipherVault.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboard;
    private readonly UserManager<IdentityUser> _users;

    public DashboardController(IDashboardService dashboard, UserManager<IdentityUser> users)
    {
        _dashboard = dashboard;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _users.GetUserId(User)!;
        var stats = await _dashboard.GetStatsAsync(userId);
        return View(stats);
    }
}
