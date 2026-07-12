using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevFlow.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(project => project.Id);

        builder.Property(project => project.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(project => project.Key)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasMaxLength(2000);

        builder.Property(project => project.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(project => project.Key)
            .IsUnique();

        builder.HasMany(project => project.Members)
            .WithOne(member => member.Project)
            .HasForeignKey(member => member.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(project => project.Sprints)
            .WithOne(sprint => sprint.Project)
            .HasForeignKey(sprint => sprint.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(project => project.Issues)
            .WithOne(issue => issue.Project)
            .HasForeignKey(issue => issue.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(project => project.Labels)
            .WithOne(label => label.Project)
            .HasForeignKey(label => label.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}