using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevFlow.Infrastructure.Persistence.Configurations;

public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.ToTable("Labels");

        builder.HasKey(label => label.Id);

        builder.Property(label => label.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(label => label.Color)
            .HasMaxLength(20);

        builder.HasIndex(label => new
        {
            label.ProjectId,
            label.Name
        })
        .IsUnique();

        builder.HasOne(label => label.Project)
            .WithMany(project => project.Labels)
            .HasForeignKey(label => label.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}