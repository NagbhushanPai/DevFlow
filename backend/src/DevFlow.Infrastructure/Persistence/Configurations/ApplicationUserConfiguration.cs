using DevFlow.Domain.Entities;
using DevFlow.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevFlow.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration
    : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasMany<Project>()
            .WithOne()
            .HasForeignKey(project => project.OwnerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany<ProjectMember>()
            .WithOne()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany<Issue>()
            .WithOne()
            .HasForeignKey(issue => issue.ReporterId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany<Issue>()
            .WithOne()
            .HasForeignKey(issue => issue.AssigneeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany<Comment>()
            .WithOne()
            .HasForeignKey(comment => comment.AuthorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany<Attachment>()
            .WithOne()
            .HasForeignKey(attachment => attachment.UploadedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany<IssueHistory>()
            .WithOne()
            .HasForeignKey(history => history.ChangedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany<Notification>()
            .WithOne()
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<RefreshToken>()
            .WithOne()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}