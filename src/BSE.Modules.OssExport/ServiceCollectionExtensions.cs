using BSE.Modules.OssExport.Repositories;
using BSE.Modules.OssExport.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BSE.Modules.OssExport;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOssExportModule(this IServiceCollection services)
    {
        services.AddScoped<IOssExportRepository, OssExportRepository>();
        services.AddScoped<IOssExportService, OssExportService>();
        return services;
    }
}
