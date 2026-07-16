using CMS.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.Property(a => a.UserName).HasMaxLength(256);
        builder.Property(a => a.EntityName).HasMaxLength(128);
        builder.Property(a => a.EntityId).HasMaxLength(128);
        builder.Property(a => a.AffectedColumns).HasMaxLength(2048);
        builder.Property(a => a.IpAddress).HasMaxLength(64);
        builder.Property(a => a.UserAgent).HasMaxLength(512);
        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(a => a.TimestampUtc);
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.UserId);
    }
}

public sealed class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.ToTable("SystemLogs");
        builder.Property(l => l.Level).HasMaxLength(32).IsRequired();
        builder.Property(l => l.SourceContext).HasMaxLength(256);
        builder.Property(l => l.RequestPath).HasMaxLength(512);
        builder.Property(l => l.RequestId).HasMaxLength(64);
        builder.HasIndex(l => l.TimestampUtc);
        builder.HasIndex(l => l.Level);
    }
}

public sealed class JobRunConfiguration : IEntityTypeConfiguration<JobRun>
{
    public void Configure(EntityTypeBuilder<JobRun> builder)
    {
        builder.ToTable("JobRuns");
        builder.Property(j => j.JobName).HasMaxLength(256).IsRequired();
        builder.Property(j => j.JobType).HasMaxLength(64).IsRequired();
        builder.Property(j => j.TriggeredBy).HasMaxLength(256);
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(j => new { j.JobName, j.Status });
        builder.HasIndex(j => j.StartedOnUtc);
    }
}

public sealed class ScheduledTaskConfiguration : IEntityTypeConfiguration<ScheduledTask>
{
    public void Configure(EntityTypeBuilder<ScheduledTask> builder)
    {
        builder.ToTable("ScheduledTasks");
        builder.Property(t => t.Name).HasMaxLength(128).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(512);
        builder.Property(t => t.CronExpression).HasMaxLength(64).IsRequired();
        builder.Property(t => t.JobType).HasMaxLength(256).IsRequired();
        builder.Property(t => t.LastStatus).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(t => t.Name).IsUnique();
    }
}
