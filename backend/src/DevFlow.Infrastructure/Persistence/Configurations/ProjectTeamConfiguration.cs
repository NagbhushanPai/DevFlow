using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevFlow.Infrastructure.Persistence.Configurations;

public sealed class ProjectTeamConfiguration : IEntityTypeConfiguration<ProjectTeam>
{
    public void Configure(EntityTypeBuilder<ProjectTeam> builder)
    {
        builder.ToTable("ProjectTeams");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.ProjectId, item.TeamId }).IsUnique();
        builder.HasOne(item => item.Project).WithMany(project => project.ProjectTeams)
            .HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Team).WithMany(team => team.ProjectTeams)
            .HasForeignKey(item => item.TeamId).OnDelete(DeleteBehavior.Cascade);
    }
}
