using KPI.Domain.Entities.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KPI.Infrastructure.Data.Configurations;

public class DashboardConfigConfiguration : IEntityTypeConfiguration<DashboardConfig>
{
    public void Configure(EntityTypeBuilder<DashboardConfig> builder)
    {
        builder.ToTable("dashboard_configs");
        builder.HasKey(dc => dc.Id);
        builder.Property(dc => dc.WidgetKey).HasMaxLength(50).IsRequired();
        builder.HasIndex(dc => new { dc.UserId, dc.WidgetKey }).IsUnique();

        builder.HasOne(dc => dc.User)
            .WithMany()
            .HasForeignKey(dc => dc.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ReportType).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Title).HasMaxLength(200).IsRequired();
        builder.Property(r => r.ExportFormat).HasConversion<string>().HasMaxLength(10);
        builder.Property(r => r.FileUrl).HasMaxLength(500).IsRequired();

        builder.HasOne(r => r.Period)
            .WithMany()
            .HasForeignKey(r => r.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.GeneratedByUser)
            .WithMany()
            .HasForeignKey(r => r.GeneratedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Content).IsRequired();
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.TargetUrl).HasMaxLength(255);

        builder.HasOne(n => n.Recipient)
            .WithMany()
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.RecipientId, n.IsRead }).HasDatabaseName("idx_notif_recipient");
    }
}

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");
        builder.HasKey(nt => nt.Id);
        builder.Property(nt => nt.EventCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(nt => nt.EventCode).IsUnique();
        builder.Property(nt => nt.SubjectTemplate).HasMaxLength(255).IsRequired();
        builder.Property(nt => nt.BodyTemplate).IsRequired();
        builder.Property(nt => nt.Channels).HasMaxLength(50);
    }
}

public class EmailQueueConfiguration : IEntityTypeConfiguration<EmailQueue>
{
    public void Configure(EntityTypeBuilder<EmailQueue> builder)
    {
        builder.ToTable("email_queues");
        builder.HasKey(eq => eq.Id);
        builder.Property(eq => eq.ToEmail).HasMaxLength(100).IsRequired();
        builder.Property(eq => eq.Subject).HasMaxLength(255).IsRequired();
        builder.Property(eq => eq.HtmlBody).IsRequired();
        builder.Property(eq => eq.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(eq => new { eq.Status, eq.ScheduledAt }).HasDatabaseName("idx_email_status");
    }
}
