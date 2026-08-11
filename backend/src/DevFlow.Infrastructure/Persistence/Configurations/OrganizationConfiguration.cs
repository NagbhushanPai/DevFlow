using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevFlow.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(o => o.Members)
            .WithOne(m => m.Organization)
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Teams)
            .WithOne(t => t.Organization)
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.Name);
    }
}
