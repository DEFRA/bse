using BSE.Modules.AdnsExport.Configuration;
using BSE.Modules.AdnsExport.Email;
using BSE.Modules.AdnsExport.Repositories;
using BSE.Modules.AdnsExport.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BSE.Modules.AdnsExport;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdnsExportModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AdnsMsGraphOptions>(
            configuration.GetSection(AdnsMsGraphOptions.SectionName));

        services.AddScoped<IAdnsRepository, AdnsRepository>();
        services.AddScoped<IAdnsExportService, AdnsExportService>();

        services.AddHttpClient<IMSGraphMailClient, MSGraphMailClient>();

        return services;
    }
}
