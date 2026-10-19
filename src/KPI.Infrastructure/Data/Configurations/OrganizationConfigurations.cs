using KPI.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KPI.Infrastructure.Data.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(o => o.Code).IsUnique();
        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
        builder.Property(o => o.TaxCode).HasMaxLength(20);
        builder.Property(o => o.Address).HasMaxLength(255);
        builder.Property(o => o.OrgType).HasConversion<string>().HasMaxLength(30);
        builder.Property(o => o.Status).HasMaxLength(20);
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Code).HasMaxLength(30).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(150).IsRequired();
        builder.Property(d => d.TreePath).HasMaxLength(255).IsRequired();
        builder.Property(d => d.Status).HasMaxLength(20);
        builder.HasIndex(d => new { d.OrgId, d.Code }).IsUnique();
        builder.HasIndex(d => d.TreePath).HasDatabaseName("idx_departments_tree");

        builder.HasOne(d => d.Organization)
            .WithMany(o => o.Departments)
            .HasForeignKey(d => d.OrgId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(d => d.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DepartmentHierarchyConfiguration : IEntityTypeConfiguration<DepartmentHierarchy>
{
    public void Configure(EntityTypeBuilder<DepartmentHierarchy> builder)
    {
        builder.ToTable("department_hierarchy");
        builder.HasKey(dh => new { dh.AncestorId, dh.DescendantId });

        builder.HasOne(dh => dh.Ancestor)
            .WithMany()
            .HasForeignKey(dh => dh.AncestorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dh => dh.Descendant)
            .WithMany()
            .HasForeignKey(dh => dh.DescendantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("positions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.RankLevel).HasMaxLength(30);
    }
}

public class EmployeeProfileConfiguration : IEntityTypeConfiguration<EmployeeProfile>
{
    public void Configure(EntityTypeBuilder<EmployeeProfile> builder)
    {
        builder.ToTable("employee_profiles");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.StaffCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(e => e.StaffCode).IsUnique();
        builder.Property(e => e.FullName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(20);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Position)
            .WithMany(p => p.Employees)
            .HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.DepartmentId).HasDatabaseName("idx_emp_dept");
    }
}

public class PositionRankConfiguration : IEntityTypeConfiguration<PositionRank>
{
    public void Configure(EntityTypeBuilder<PositionRank> builder)
    {
        builder.ToTable("position_ranks");
        builder.HasKey(pr => pr.RankCode);
        builder.Property(pr => pr.RankCode).HasMaxLength(20);
        builder.Property(pr => pr.RankName).HasMaxLength(100).IsRequired();
        builder.Property(pr => pr.RankGroup).HasMaxLength(50).IsRequired();
    }
}

public class CivilServantProfileConfiguration : IEntityTypeConfiguration<CivilServantProfile>
{
    public void Configure(EntityTypeBuilder<CivilServantProfile> builder)
    {
        builder.ToTable("civil_servant_profiles");
        builder.HasKey(cs => cs.Id);
        builder.Property(cs => cs.CivilServantCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(cs => cs.CivilServantCode).IsUnique();
        builder.Property(cs => cs.OfficialRankCode).HasMaxLength(20).IsRequired();
        builder.Property(cs => cs.SalaryCoefficient).HasPrecision(5, 2);
        builder.Property(cs => cs.PartyStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(cs => cs.PoliticalTheory).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(cs => cs.Employee)
            .WithOne(e => e.CivilServantProfile)
            .HasForeignKey<CivilServantProfile>(cs => cs.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cs => cs.OfficialRank)
            .WithMany(r => r.CivilServants)
            .HasForeignKey(cs => cs.OfficialRankCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
