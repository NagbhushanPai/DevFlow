using DevFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevFlow.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Action).HasMaxLength(100).IsRequired();
        builder.Property(log => log.HttpMethod).HasMaxLength(10).IsRequired();
        builder.Property(log => log.Path).HasMaxLength(500).IsRequired();
        builder.Property(log => log.IpAddress).HasMaxLength(100);
        builder.Property(log => log.UserAgent).HasMaxLength(1000);
        builder.Property(log => log.TraceId).HasMaxLength(100);
        builder.HasIndex(log => new { log.CreatedAtUtc, log.UserId });
    }
}
