using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevFlow.Infrastructure.Persistence.Configurations;

public class IssueLinkConfiguration : IEntityTypeConfiguration<IssueLink>
{
    public void Configure(EntityTypeBuilder<IssueLink> builder)
    {
        builder.ToTable("IssueLinks");

        builder.HasKey(issueLink => issueLink.Id);

        builder.Property(issueLink => issueLink.LinkType)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(issueLink => new
        {
            issueLink.SourceIssueId,
            issueLink.TargetIssueId,
            issueLink.LinkType
        })
        .IsUnique();

        builder.HasOne(issueLink => issueLink.SourceIssue)
            .WithMany(issue => issue.OutgoingLinks)
            .HasForeignKey(issueLink => issueLink.SourceIssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(issueLink => issueLink.TargetIssue)
            .WithMany(issue => issue.IncomingLinks)
            .HasForeignKey(issueLink => issueLink.TargetIssueId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}