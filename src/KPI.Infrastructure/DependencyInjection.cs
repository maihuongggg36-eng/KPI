using KPI.Application.Common.Interfaces;
using KPI.Domain.Interfaces;
using KPI.Infrastructure.Data;
using KPI.Infrastructure.Data.Interceptors;
using KPI.Infrastructure.Repositories;
using KPI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Database=KPI_System;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<KpiDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();
            options.UseSqlServer(connectionString, b =>
            {
                b.MigrationsAssembly(typeof(KpiDbContext).Assembly.FullName);
                b.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            });
            options.AddInterceptors(interceptor);
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<KPI.Application.Services.IDashboardService, DashboardService>();
        services.AddScoped<KPI.Application.Services.ICatalogService, CatalogService>();
        services.AddScoped<KPI.Application.Services.IKpiPlanningService, KpiPlanningService>();
        services.AddScoped<KPI.Application.Services.IEvaluationService, EvaluationService>();
        services.AddScoped<KPI.Application.Services.IDecree90Service, Decree90Service>();
        services.AddScoped<KPI.Application.Services.IPayrollService, PayrollService>();
        services.AddScoped<KPI.Application.Services.IReportService, ReportService>();
        services.AddScoped<KPI.Application.Services.IEmailService, EmailService>();
        services.AddScoped<KPI.Application.Services.INotificationService, NotificationService>();
        services.AddScoped<KPI.Application.Services.IProfileService, ProfileService>();
        services.AddScoped<KPI.Application.Services.ISearchService, SearchService>();
        services.AddScoped<KPI.Application.Services.IAdminService, AdminService>();
        services.AddScoped<KPI.Application.Services.IMenuService, MenuService>();
        services.AddScoped<KPI.Application.Services.IAttendanceService, AttendanceService>();
        services.AddScoped<KPI.Application.Services.IDirectorTargetService, DirectorTargetService>();
        services.AddHostedService<EvaluationDeadlineBackgroundService>();
        services.AddHostedService<EmailQueueBackgroundService>();

        return services;
    }
}
