using BSE.Modules.AuditLog.Repositories;
using BSE.Modules.AuditLog.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BSE.Modules.AuditLog;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Audit Log module services into the DI container.
    /// Call from <c>BSE.Host/Program.cs</c>.
    /// </summary>
    public static IServiceCollection AddAuditLogModule(this IServiceCollection services)
    {
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        return services;
    }
}
