using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevFlow.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration
    : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(attachment => attachment.FileUrl)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(attachment => attachment.ContentType)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(attachment => attachment.FileSize)
            .IsRequired();

        builder.HasOne(attachment => attachment.Issue)
            .WithMany(issue => issue.Attachments)
            .HasForeignKey(attachment => attachment.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(attachment => attachment.IssueId);
    }
}