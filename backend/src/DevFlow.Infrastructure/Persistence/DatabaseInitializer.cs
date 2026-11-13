using DevFlow.Application.Common.Authorization;
using DevFlow.Domain.Entities;
using DevFlow.Domain.Enums;
using DevFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DevFlow.Infrastructure.Persistence;

/// <summary>
/// One-shot database setup: applies migrations, seeds roles and (optionally) demo data.
/// Run it from a single process (e.g. <c>dotnet DevFlow.API.dll --migrate</c>) rather than from every API
/// replica, so concurrent instances never race on schema changes.
/// </summary>
public static class DatabaseInitializer
{
    public const string AdminEmail = "admin@devflow.local";
    public const string AdminPassword = "Admin@12345";
    public const string DeveloperEmail = "dev@devflow.local";
    public const string DeveloperPassword = "Developer@123";
    public const string ViewerEmail = "viewer@devflow.local";
    public const string ViewerPassword = "Viewer@12345";

    public static async Task InitializeAsync(IServiceProvider services, bool applyMigrations, bool seedDemoData, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));
        var db = provider.GetRequiredService<ApplicationDbContext>();

        if (applyMigrations && db.Database.IsRelational())
        {
            logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync(ct);
        }

        await SeedRolesAsync(provider, logger);

        if (seedDemoData)
        {
            await SeedDemoDataAsync(provider, db, logger, ct);
        }
    }

    public static async Task SeedRolesAsync(IServiceProvider provider, ILogger logger)
    {
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { Roles.Admin, Roles.Manager, Roles.Developer })
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            if (!result.Succeeded && !await roleManager.RoleExistsAsync(role))
            {
                logger.LogError("Failed to create role {Role}: {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task SeedDemoDataAsync(IServiceProvider provider, ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(AdminEmail) is not null)
        {
            logger.LogInformation("Demo data already present; skipping.");
            return;
        }

        logger.LogInformation("Seeding demo data...");
        var admin = await CreateUserAsync(userManager, "Ada", "Admin", AdminEmail, AdminPassword, Roles.Admin, Roles.Manager);
        var developer = await CreateUserAsync(userManager, "Dev", "Eloper", DeveloperEmail, DeveloperPassword, Roles.Developer);
        var viewer = await CreateUserAsync(userManager, "Vera", "Viewer", ViewerEmail, ViewerPassword, Roles.Developer);

        var now = DateTime.UtcNow;
        var organization = new Organization
        {
            Name = "Acme Software",
            Description = "Demo organization created by the seeder.",
            OwnerId = admin.Id,
            CreatedAtUtc = now,
        };
        organization.Members.Add(new OrganizationMember { UserId = admin.Id, Role = OrganizationRoles.Owner });
        organization.Members.Add(new OrganizationMember { UserId = developer.Id, Role = OrganizationRoles.Member });
        organization.Members.Add(new OrganizationMember { UserId = viewer.Id, Role = OrganizationRoles.Member });

        var team = new Team { Name = "Platform", Description = "Core platform team", CreatedAtUtc = now };
        team.Members.Add(new TeamMember { UserId = admin.Id, Role = TeamRoles.Lead });
        team.Members.Add(new TeamMember { UserId = developer.Id, Role = TeamRoles.Developer });
        organization.Teams.Add(team);

        var project = new Project
        {
            Name = "DevFlow Web",
            Key = "WEB",
            Description = "Customer-facing web application.",
            Status = ProjectStatus.Active,
            OwnerId = admin.Id,
            CreatedAtUtc = now,
        };
        project.Members.Add(new ProjectMember { UserId = admin.Id, Role = ProjectMemberRole.Manager, JoinedAtUtc = now });
        project.Members.Add(new ProjectMember { UserId = developer.Id, Role = ProjectMemberRole.Developer, JoinedAtUtc = now });
        project.Members.Add(new ProjectMember { UserId = viewer.Id, Role = ProjectMemberRole.Viewer, JoinedAtUtc = now });
        project.ProjectTeams.Add(new ProjectTeam { Team = team, AssignedAtUtc = now });
        organization.Projects.Add(project);

        var sprint = new Sprint
        {
            Name = "Sprint 1",
            Goal = "Ship the login and board experience",
            Status = SprintStatus.Active,
            StartDateUtc = now.Date.AddDays(-3),
            EndDateUtc = now.Date.AddDays(11),
            CreatedAtUtc = now,
        };
        project.Sprints.Add(sprint);

        var issues = new (string Title, IssueType Type, IssueStatus Status, IssuePriority Priority, Guid? Assignee, bool InSprint)[]
        {
            ("Set up CI pipeline", IssueType.Task, IssueStatus.Done, IssuePriority.High, admin.Id, true),
            ("Login page shows wrong error on lockout", IssueType.Bug, IssueStatus.InProgress, IssuePriority.Critical, developer.Id, true),
            ("Drag and drop on the board", IssueType.Story, IssueStatus.InReview, IssuePriority.Medium, developer.Id, true),
            ("Dark mode", IssueType.Story, IssueStatus.Todo, IssuePriority.Low, null, true),
            ("Email digest of notifications", IssueType.Story, IssueStatus.Backlog, IssuePriority.Medium, null, false),
            ("Attachment previews for images", IssueType.Task, IssueStatus.Backlog, IssuePriority.Low, null, false),
        };

        var number = 1;
        foreach (var (title, type, status, priority, assignee, inSprint) in issues)
        {
            project.Issues.Add(new Issue
            {
                IssueNumber = number++,
                Title = title,
                Description = $"Demo issue: {title}.",
                Type = type,
                Status = status,
                Priority = priority,
                ReporterId = admin.Id,
                AssigneeId = assignee,
                Sprint = inSprint ? sprint : null,
                CreatedAtUtc = now,
            });
        }

        project.NextIssueNumber = number;

        db.Organizations.Add(organization);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Demo data seeded.");
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string firstName,
        string lastName,
        string email,
        string password,
        params string[] roles)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRolesAsync(user, roles);
        return user;
    }
}
