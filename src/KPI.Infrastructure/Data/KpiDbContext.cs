using System.Reflection;
using KPI.Domain.Common;
using KPI.Domain.Entities.Catalogs;
using KPI.Domain.Entities.Evaluation;
using KPI.Domain.Entities.Identity;
using KPI.Domain.Entities.Organization;
using KPI.Domain.Entities.Planning;
using KPI.Domain.Entities.Reporting;
using KPI.Domain.Entities.Payroll;
using Microsoft.EntityFrameworkCore;

namespace KPI.Infrastructure.Data;

/// <summary>
/// DbContext trung tâm quản lý toàn bộ 40 bảng thực thể của hệ thống KPI
/// </summary>
public class KpiDbContext : DbContext
{
    public KpiDbContext(DbContextOptions<KpiDbContext> options) : base(options)
    {
    }

    // Phân hệ 1: Quản trị, Phân quyền & Bảo mật
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    // Phân hệ 2: Cơ cấu Tổ chức & Hồ sơ Nhân sự / Công chức
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<DepartmentHierarchy> DepartmentHierarchies => Set<DepartmentHierarchy>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
    public DbSet<PositionRank> PositionRanks => Set<PositionRank>();
    public DbSet<CivilServantProfile> CivilServantProfiles => Set<CivilServantProfile>();

    // Phân hệ 3: Danh mục Dùng chung & Kỳ Đánh giá
    public DbSet<KpiCategory> KpiCategories => Set<KpiCategory>();
    public DbSet<MeasurementUnit> MeasurementUnits => Set<MeasurementUnit>();
    public DbSet<MeasurementMethod> MeasurementMethods => Set<MeasurementMethod>();
    public DbSet<KpiMasterItem> KpiMasterItems => Set<KpiMasterItem>();
    public DbSet<ScoreScale> ScoreScales => Set<ScoreScale>();
    public DbSet<ClassificationLevel> ClassificationLevels => Set<ClassificationLevel>();
    public DbSet<EvaluationPeriod> EvaluationPeriods => Set<EvaluationPeriod>();

    // Phân hệ 4: Thiết lập & Giao Chỉ tiêu KPI
    public DbSet<KpiTemplate> KpiTemplates => Set<KpiTemplate>();
    public DbSet<KpiTemplateDetail> KpiTemplateDetails => Set<KpiTemplateDetail>();
    public DbSet<KpiAssignment> KpiAssignments => Set<KpiAssignment>();
    public DbSet<KpiAssignmentDetail> KpiAssignmentDetails => Set<KpiAssignmentDetail>();
    public DbSet<KpiEvidence> KpiEvidences => Set<KpiEvidence>();
    public DbSet<DirectorTarget> DirectorTargets => Set<DirectorTarget>();

    // Phân hệ 5: Đánh giá, Công chức & Workflow
    public DbSet<Evaluation> Evaluations => Set<Evaluation>();
    public DbSet<EvaluationDetail> EvaluationDetails => Set<EvaluationDetail>();
    public DbSet<CivilServantClassification> CivilServantClassifications => Set<CivilServantClassification>();
    public DbSet<PeerReview> PeerReviews => Set<PeerReview>();
    public DbSet<EvaluationWorkflow> EvaluationWorkflows => Set<EvaluationWorkflow>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<WorkflowHistory> WorkflowHistories => Set<WorkflowHistory>();
    public DbSet<EvaluationDispute> EvaluationDisputes => Set<EvaluationDispute>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    // Phân hệ 6: Báo cáo, Dashboard & Thông báo
    public DbSet<DashboardConfig> DashboardConfigs => Set<DashboardConfig>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<EmailQueue> EmailQueues => Set<EmailQueue>();

    // Phân hệ 7: Lương 3P & Chi trả tiền mặt
    public DbSet<PayrollRecord> Payrolls => Set<PayrollRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("menu_items");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.ToTable("attendance_records");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DirectorTarget>(entity =>
        {
            entity.ToTable("director_targets");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Period)
                .WithMany()
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayrollRecord>(entity =>
        {
            entity.ToTable("payrolls");
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Period)
                .WithMany()
                .HasForeignKey(e => e.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Evaluation)
                .WithMany()
                .HasForeignKey(e => e.EvaluationId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Cashier)
                .WithMany()
                .HasForeignKey(e => e.CashierId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Thiết lập bộ lọc xóa mềm tự động (Global Query Filter cho ISoftDeletable)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var body = System.Linq.Expressions.Expression.Equal(
                    System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)),
                    System.Linq.Expressions.Expression.Constant(false));
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(System.Linq.Expressions.Expression.Lambda(body, parameter));
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
