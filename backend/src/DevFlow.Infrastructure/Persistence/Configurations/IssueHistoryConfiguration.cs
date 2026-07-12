using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevFlow.Infrastructure.Persistence.Configurations;

public class IssueHistoryConfiguration
    : IEntityTypeConfiguration<IssueHistory>
{
    public void Configure(EntityTypeBuilder<IssueHistory> builder)
    {
        builder.ToTable("IssueHistories");

        builder.HasKey(history => history.Id);

        builder.Property(history => history.FieldName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(history => history.OldValue)
            .HasMaxLength(2000);

        builder.Property(history => history.NewValue)
            .HasMaxLength(2000);

        builder.HasOne(history => history.Issue)
            .WithMany(issue => issue.History)
            .HasForeignKey(history => history.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(history => history.IssueId);
    }
}