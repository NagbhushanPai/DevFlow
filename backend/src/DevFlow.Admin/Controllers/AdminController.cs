using DevFlow.Infrastructure.Persistence;
using DevFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Admin.Controllers;

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
