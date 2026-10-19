using System.IO.Compression;
using System.Threading.RateLimiting;
using KPI.Application;
using KPI.Application.Common.Interfaces;
using KPI.Application.Common.Security;
using KPI.Infrastructure;
using KPI.Infrastructure.Data;
using KPI.Infrastructure.Data.Seed;
using KPI.Infrastructure.Security;
using KPI.Web.Health;
using KPI.Web.Middleware;
using KPI.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Serilog đọc cấu hình từ appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Đang khởi động Hệ thống Quản lý KPI - Học viện Hành chính và Quản trị công...");

    // 1. Đăng ký các tầng trong kiến trúc Clean Architecture
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // 2. Dịch vụ Tầng Web & Data Protection
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddScoped<IDateTime, DateTimeService>();
    builder.Services.AddDataProtection();

    // 3. Tối ưu Hiệu năng: Response Compression (Brotli & Gzip) & In-Memory Caching
    builder.Services.AddMemoryCache();
    builder.Services.AddResponseCaching();
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
        {
            "application/json",
            "text/html",
            "text/css",
            "application/javascript",
            "text/javascript",
            "image/svg+xml"
        });
    });
    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest;
    });
    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest;
    });

    // 4. Cấu hình Health Checks đa thành phần (Database, Memory, Disk Space)
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("Database_SqlServer")
        .AddCheck<MemoryHealthCheck>("Process_Memory")
        .AddCheck<DiskSpaceHealthCheck>("System_DiskSpace");

    // 5. Cấu hình Swagger / OpenAPI Documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Hệ thống KPI & NĐ 90 - Học viện Hành chính và Quản trị công",
            Version = "v1",
            Description = "RESTful API Quản lý Giao chỉ tiêu, Tự đánh giá, Thẩm định, Xếp loại CBCC và Lương 3P.",
            Contact = new OpenApiContact
            {
                Name = "Ban Công nghệ Thông tin - Học viện NAPA",
                Email = "cntt@napa.vn"
            }
        });
    });

    // 6. Cấu hình Xác thực Cookie Authentication
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Auth/Login";
            options.LogoutPath = "/Auth/Logout";
            options.AccessDeniedPath = "/Auth/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

    // 7. Cấu hình Phân quyền RBAC & Data Scope
    builder.Services.AddScoped<IAuthorizationHandler, DataScopeHandler>();
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdmin", policy => policy.RequireRole(AppRoles.Admin));
        options.AddPolicy("RequireDirector", policy => policy.RequireRole(AppRoles.Director, AppRoles.Admin));
        options.AddPolicy("RequireManager", policy => policy.RequireRole(AppRoles.Manager, AppRoles.Director, AppRoles.Admin));
        options.AddPolicy("RequireHr", policy => policy.RequireRole(AppRoles.HrAdmin, AppRoles.Admin));
        options.AddPolicy("RequireAccountant", policy => policy.RequireRole(AppRoles.Accountant, AppRoles.Admin));

        options.AddPolicy("ScopeDepartment", policy =>
            policy.Requirements.Add(new DataScopeRequirement(DataScope.Department)));
        options.AddPolicy("ScopeOrganization", policy =>
            policy.Requirements.Add(new DataScopeRequirement(DataScope.Organization)));
    });

    // 8. Cấu hình Chống tấn công Brute Force bằng Rate Limiter
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("loginLimiter", limiterOptions =>
        {
            limiterOptions.PermitLimit = 15;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 2;
        });
    });

    // 9. Cấu hình Anti-Forgery Token
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

    // 10. Cấu hình SignalR Real-time Notifications
    builder.Services.AddSignalR();
    builder.Services.AddScoped<KPI.Application.Services.IRealtimeNotifier, KPI.Web.Services.SignalRRealtimeNotifier>();

    builder.Services.AddControllersWithViews();

    var app = builder.Build();

    // Tự động kiểm tra và khởi tạo Database / Nạp Seed Data
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        try
        {
            var dbContext = services.GetRequiredService<KpiDbContext>();
            logger.LogInformation("Kiểm tra kết nối và cấu trúc Cơ sở dữ liệu...");
            
            if (dbContext.Database.CanConnect())
            {
                logger.LogInformation("Kết nối CSDL thành công! Đang đồng bộ cấu trúc bảng và Seed Data...");
                await dbContext.Database.EnsureCreatedAsync();
                await DbInitializer.SeedAsync(dbContext, logger);
            }
            else
            {
                logger.LogWarning("Chưa thể kết nối trực tiếp đến SQL Server. File script T-SQL có sẵn tại d:\\KPI\\database\\schema_kpi_system_sqlserver.sql.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Lưu ý kết nối CSDL: {Message}", ex.Message);
        }
    }

    // Middleware Pipeline
    app.UseMiddleware<RequestPerformanceMiddleware>();
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();

    app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseResponseCompression();
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseStaticFiles();

    app.UseRouting();
    app.UseResponseCaching();
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<AuditLoggingMiddleware>();

    // Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "KPI System API v1");
        c.RoutePrefix = "swagger";
    });

    // Endpoints
    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");
    app.MapHub<KPI.Web.Hubs.NotificationHub>("/hubs/notification");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (HostAbortedException)
{
    // Bỏ qua ngoại lệ nội bộ của EF Core command-line tools
}
catch (Exception ex)
{
    Log.Fatal(ex, "Ứng dụng bị dừng đột ngột do lỗi nghiêm trọng!");
}
finally
{
    Log.CloseAndFlush();
}
