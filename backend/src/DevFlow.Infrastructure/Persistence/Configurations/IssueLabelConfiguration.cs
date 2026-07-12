using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevFlow.Infrastructure.Persistence.Configurations;

public class IssueLabelConfiguration : IEntityTypeConfiguration<IssueLabel>
{
    public void Configure(EntityTypeBuilder<IssueLabel> builder)
    {
        builder.ToTable("IssueLabels");

        builder.HasKey(issueLabel => issueLabel.Id);

        builder.HasIndex(issueLabel => new
        {
            issueLabel.IssueId,
            issueLabel.LabelId
        })
        .IsUnique();

        builder.HasOne(issueLabel => issueLabel.Issue)
            .WithMany(issue => issue.IssueLabels)
            .HasForeignKey(issueLabel => issueLabel.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(issueLabel => issueLabel.Label)
            .WithMany(label => label.IssueLabels)
            .HasForeignKey(issueLabel => issueLabel.LabelId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}