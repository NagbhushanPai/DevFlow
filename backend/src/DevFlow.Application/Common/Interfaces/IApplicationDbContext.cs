using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevFlow.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Project> Projects { get; }

    DbSet<ProjectMember> ProjectMembers { get; }

    DbSet<Sprint> Sprints { get; }

    DbSet<Issue> Issues { get; }

    DbSet<Comment> Comments { get; }

    DbSet<Attachment> Attachments { get; }

    DbSet<IssueHistory> IssueHistories { get; }

    DbSet<Label> Labels { get; }

    DbSet<IssueLabel> IssueLabels { get; }

    DbSet<IssueLink> IssueLinks { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}