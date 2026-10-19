using System.Reflection;
using FluentValidation;
using KPI.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // Đăng ký các Domain Business Services
        services.AddScoped<IKpiCalculationService, KpiCalculationService>();

        return services;
    }
}
