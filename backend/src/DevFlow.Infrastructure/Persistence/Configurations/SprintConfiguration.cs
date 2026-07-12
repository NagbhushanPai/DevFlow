using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevFlow.Infrastructure.Persistence.Configurations;

public class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> builder)
    {
        builder.ToTable("Sprints");

        builder.HasKey(sprint => sprint.Id);

        builder.Property(sprint => sprint.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(sprint => sprint.Goal)
            .HasMaxLength(1000);

        builder.Property(sprint => sprint.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(sprint => sprint.Project)
            .WithMany(project => project.Sprints)
            .HasForeignKey(sprint => sprint.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}