using KPI.Domain.Entities.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KPI.Infrastructure.Data.Configurations;

public class KpiTemplateConfiguration : IEntityTypeConfiguration<KpiTemplate>
{
    public void Configure(EntityTypeBuilder<KpiTemplate> builder)
    {
        builder.ToTable("kpi_templates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Status).HasMaxLength(20);

        builder.HasOne(t => t.Position)
            .WithMany()
            .HasForeignKey(t => t.PositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Department)
            .WithMany()
            .HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class KpiTemplateDetailConfiguration : IEntityTypeConfiguration<KpiTemplateDetail>
{
    public void Configure(EntityTypeBuilder<KpiTemplateDetail> builder)
    {
        builder.ToTable("kpi_template_details");
        builder.HasKey(td => td.Id);
        builder.Property(td => td.TargetValue).HasPrecision(15, 2);
        builder.Property(td => td.MinThreshold).HasPrecision(15, 2);
        builder.Property(td => td.Weight).HasPrecision(5, 2);

        builder.HasOne(td => td.Template)
            .WithMany(t => t.TemplateDetails)
            .HasForeignKey(td => td.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(td => td.KpiItem)
            .WithMany()
            .HasForeignKey(td => td.KpiItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class KpiAssignmentConfiguration : IEntityTypeConfiguration<KpiAssignment>
{
    public void Configure(EntityTypeBuilder<KpiAssignment> builder)
    {
        builder.ToTable("kpi_assignments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
        builder.Property(a => a.TotalWeight).HasPrecision(5, 2);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(a => new { a.PeriodId, a.EmployeeId }).IsUnique();
        builder.HasIndex(a => a.EmployeeId).HasDatabaseName("idx_assign_emp");
        builder.HasIndex(a => a.PeriodId).HasDatabaseName("idx_assign_period");

        builder.HasOne(a => a.Period)
            .WithMany()
            .HasForeignKey(a => a.PeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Approver)
            .WithMany()
            .HasForeignKey(a => a.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class KpiAssignmentDetailConfiguration : IEntityTypeConfiguration<KpiAssignmentDetail>
{
    public void Configure(EntityTypeBuilder<KpiAssignmentDetail> builder)
    {
        builder.ToTable("kpi_assignment_details");
        builder.HasKey(ad => ad.Id);
        builder.Property(ad => ad.KpiName).HasMaxLength(255).IsRequired();
        builder.Property(ad => ad.Unit).HasMaxLength(30).IsRequired();
        builder.Property(ad => ad.TargetValue).HasPrecision(15, 2);
        builder.Property(ad => ad.MinThreshold).HasPrecision(15, 2);
        builder.Property(ad => ad.MaxCap).HasPrecision(5, 2);
        builder.Property(ad => ad.Weight).HasPrecision(5, 2);
        builder.Property(ad => ad.ScoringFormula).HasMaxLength(30).IsRequired();

        builder.HasOne(ad => ad.Assignment)
            .WithMany(a => a.AssignmentDetails)
            .HasForeignKey(ad => ad.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ad => ad.KpiItem)
            .WithMany()
            .HasForeignKey(ad => ad.KpiItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class KpiEvidenceConfiguration : IEntityTypeConfiguration<KpiEvidence>
{
    public void Configure(EntityTypeBuilder<KpiEvidence> builder)
    {
        builder.ToTable("kpi_evidences");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FileName).HasMaxLength(255).IsRequired();
        builder.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(e => e.FileType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(255);

        builder.HasOne(e => e.AssignmentDetail)
            .WithMany(ad => ad.Evidences)
            .HasForeignKey(e => e.AssignmentDetailId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
