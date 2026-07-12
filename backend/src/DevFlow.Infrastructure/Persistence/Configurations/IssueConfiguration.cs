using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevFlow.Infrastructure.Persistence.Configurations;

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.ToTable("Issues");

        builder.HasKey(issue => issue.Id);

        builder.Property(issue => issue.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(issue => issue.Description)
            .HasMaxLength(5000);

        builder.Property(issue => issue.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(issue => issue.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(issue => issue.Priority)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(issue => issue.IssueNumber)
            .IsRequired();

        builder.HasIndex(issue => new
        {
            issue.ProjectId,
            issue.IssueNumber
        })
        .IsUnique();

        builder.HasOne(issue => issue.Project)
            .WithMany(project => project.Issues)
            .HasForeignKey(issue => issue.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(issue => issue.Sprint)
            .WithMany(sprint => sprint.Issues)
            .HasForeignKey(issue => issue.SprintId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}