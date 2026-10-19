using System.Text.Json;
using KPI.Application.Common.Interfaces;
using KPI.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KPI.Infrastructure.Data.Interceptors;

/// <summary>
/// Interceptor tự động ghi vết thay đổi dữ liệu (Audit Trail) cho toàn bộ thao tác INSERT / UPDATE / DELETE
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditSaveChangesInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not KpiDbContext context)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var auditEntries = new List<AuditLog>();
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        var userId = _currentUserService.UserId;

        foreach (var entry in entries)
        {
            var entityName = entry.Metadata.ClrType.Name;
            var primaryKey = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue;
            var entityId = primaryKey != null && long.TryParse(primaryKey.ToString(), out var id) ? id : 0;

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            switch (entry.State)
            {
                case EntityState.Added:
                    foreach (var prop in entry.Properties)
                    {
                        if (!prop.Metadata.IsPrimaryKey())
                        {
                            newValues[prop.Metadata.Name] = prop.CurrentValue;
                        }
                    }
                    auditEntries.Add(new AuditLog
                    {
                        UserId = userId,
                        Action = "INSERT",
                        EntityName = entityName,
                        EntityId = entityId,
                        NewValues = JsonSerializer.Serialize(newValues),
                        IpAddress = "127.0.0.1",
                        CreatedAt = DateTime.UtcNow
                    });
                    break;

                case EntityState.Modified:
                    foreach (var prop in entry.Properties)
                    {
                        if (prop.IsModified)
                        {
                            oldValues[prop.Metadata.Name] = prop.OriginalValue;
                            newValues[prop.Metadata.Name] = prop.CurrentValue;
                        }
                    }

                    if (oldValues.Count > 0)
                    {
                        auditEntries.Add(new AuditLog
                        {
                            UserId = userId,
                            Action = "UPDATE",
                            EntityName = entityName,
                            EntityId = entityId,
                            OldValues = JsonSerializer.Serialize(oldValues),
                            NewValues = JsonSerializer.Serialize(newValues),
                            IpAddress = "127.0.0.1",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    break;

                case EntityState.Deleted:
                    foreach (var prop in entry.Properties)
                    {
                        oldValues[prop.Metadata.Name] = prop.OriginalValue;
                    }
                    auditEntries.Add(new AuditLog
                    {
                        UserId = userId,
                        Action = "DELETE",
                        EntityName = entityName,
                        EntityId = entityId,
                        OldValues = JsonSerializer.Serialize(oldValues),
                        IpAddress = "127.0.0.1",
                        CreatedAt = DateTime.UtcNow
                    });
                    break;
            }
        }

        if (auditEntries.Count > 0)
        {
            context.AuditLogs.AddRange(auditEntries);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
