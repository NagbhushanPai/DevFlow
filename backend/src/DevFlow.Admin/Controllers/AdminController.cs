using DevFlow.Domain.Enums;
using DevFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Admin.Controllers;

/// <summary>Admin dashboard. Access is restricted to the global Admin role by the fallback policy in Program.cs.</summary>
public sealed class AdminController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var model = new DashboardViewModel(
            await db.Organizations.CountAsync(ct),
            await db.Projects.CountAsync(p => p.Status != ProjectStatus.Archived, ct),
            await db.Issues.CountAsync(i => !i.IsDeleted, ct),
            await db.AuditLogs.OrderByDescending(log => log.CreatedAtUtc).Take(20)
                .Select(log => new AuditRow(log.CreatedAtUtc, log.Action, log.StatusCode, log.UserId, log.IpAddress))
                .ToListAsync(ct));
        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Error() => View();

    [HttpGet]
    public async Task<IActionResult> Reports(CancellationToken ct) =>
        Json(new
        {
            organizations = await db.Organizations.CountAsync(ct),
            projects = await db.Projects.CountAsync(ct),
            issues = await db.Issues.CountAsync(i => !i.IsDeleted, ct),
            auditEvents = await db.AuditLogs.CountAsync(ct)
        });
}

public sealed record DashboardViewModel(int Organizations, int ActiveProjects, int OpenIssues, IReadOnlyList<AuditRow> RecentAudit);
public sealed record AuditRow(DateTime CreatedAtUtc, string Action, int StatusCode, Guid? UserId, string? IpAddress);
