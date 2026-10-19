using KPI.Domain.Entities.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KPI.Infrastructure.Data.Configurations;

public class KpiCategoryConfiguration : IEntityTypeConfiguration<KpiCategory>
{
    public void Configure(EntityTypeBuilder<KpiCategory> builder)
    {
        builder.ToTable("kpi_categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Perspective).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.TargetSector).HasConversion<string>().HasMaxLength(30);
    }
}

public class MeasurementUnitConfiguration : IEntityTypeConfiguration<MeasurementUnit>
{
    public void Configure(EntityTypeBuilder<MeasurementUnit> builder)
    {
        builder.ToTable("measurement_units");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(u => u.Code).IsUnique();
        builder.Property(u => u.Name).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Symbol).HasMaxLength(15).IsRequired();
    }
}

public class MeasurementMethodConfiguration : IEntityTypeConfiguration<MeasurementMethod>
{
    public void Configure(EntityTypeBuilder<MeasurementMethod> builder)
    {
        builder.ToTable("measurement_methods");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(m => m.Code).IsUnique();
        builder.Property(m => m.Name).HasMaxLength(100).IsRequired();
        builder.Property(m => m.DataSource).HasMaxLength(100);
    }
}

public class KpiMasterItemConfiguration : IEntityTypeConfiguration<KpiMasterItem>
{
    public void Configure(EntityTypeBuilder<KpiMasterItem> builder)
    {
        builder.ToTable("kpi_master_items");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(k => k.Code).IsUnique();
        builder.Property(k => k.Name).HasMaxLength(255).IsRequired();
        builder.Property(k => k.ScoringFormula).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(k => k.Category)
            .WithMany(c => c.MasterItems)
            .HasForeignKey(k => k.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(k => k.Unit)
            .WithMany(u => u.MasterItems)
            .HasForeignKey(k => k.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(k => k.Method)
            .WithMany(m => m.MasterItems)
            .HasForeignKey(k => k.MethodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ScoreScaleConfiguration : IEntityTypeConfiguration<ScoreScale>
{
    public void Configure(EntityTypeBuilder<ScoreScale> builder)
    {
        builder.ToTable("score_scales");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.MinScore).HasPrecision(5, 2);
        builder.Property(s => s.MaxScore).HasPrecision(5, 2);
    }
}

public class ClassificationLevelConfiguration : IEntityTypeConfiguration<ClassificationLevel>
{
    public void Configure(EntityTypeBuilder<ClassificationLevel> builder)
    {
        builder.ToTable("classification_levels");
        builder.HasKey(cl => cl.Id);
        builder.Property(cl => cl.Code).HasMaxLength(30).IsRequired();
        builder.Property(cl => cl.Name).HasMaxLength(100).IsRequired();
        builder.Property(cl => cl.MinPercentage).HasPrecision(5, 2);
        builder.Property(cl => cl.MaxPercentage).HasPrecision(5, 2);

        builder.HasOne(cl => cl.Scale)
            .WithMany(s => s.ClassificationLevels)
            .HasForeignKey(cl => cl.ScaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EvaluationPeriodConfiguration : IEntityTypeConfiguration<EvaluationPeriod>
{
    public void Configure(EntityTypeBuilder<EvaluationPeriod> builder)
    {
        builder.ToTable("evaluation_periods");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.PeriodType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(p => p.Scale)
            .WithMany(s => s.EvaluationPeriods)
            .HasForeignKey(p => p.ScaleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
