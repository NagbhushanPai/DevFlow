using DevFlow.Application.Common.Interfaces;
using DevFlow.Domain.Entities;
using DevFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DevFlow.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>,
      IApplicationDbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<ProjectTeam> ProjectTeams => Set<ProjectTeam>();

    public DbSet<Sprint> Sprints => Set<Sprint>();

    public DbSet<Issue> Issues => Set<Issue>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<IssueHistory> IssueHistories => Set<IssueHistory>();

    public DbSet<Label> Labels => Set<Label>();

    public DbSet<IssueLabel> IssueLabels => Set<IssueLabel>();

    public DbSet<IssueLink> IssueLinks => Set<IssueLink>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Organization and Team entities
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    /// <summary>
    /// All timestamps are stored as UTC, but SQL Server's datetime2 has no offset, so EF would materialise them
    /// with <see cref="DateTimeKind.Unspecified"/> and the API would serialise them without a "Z" — browsers
    /// then read them as local time. Marking every DateTime as UTC on read fixes this everywhere.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
        value => value.HasValue ? (value.Value.Kind == DateTimeKind.Utc ? value : value.Value.ToUniversalTime()) : value,
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
