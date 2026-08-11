using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevFlow.Infrastructure.Persistence.Configurations;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.TeamId, m.UserId }).IsUnique();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(m => m.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
