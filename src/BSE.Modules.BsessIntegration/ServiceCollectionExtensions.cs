using BSE.Modules.BsessIntegration.Configuration;
using BSE.Modules.BsessIntegration.Jobs;
using BSE.Modules.BsessIntegration.Repositories;
using BSE.Modules.BsessIntegration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BSE.Modules.BsessIntegration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBsessIntegrationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BsessEtlOptions>(configuration.GetSection(BsessEtlOptions.SectionName));

        services.AddScoped<IBsessRepository, BsessRepository>();
        services.AddScoped<IBsessCheckService, BsessCheckService>();

        // Singleton: BsessEtlService only depends on IOptions, IConfiguration, and ILogger —
        // all Singleton-safe — so it can be shared with the BackgroundService host.
        services.AddSingleton<IBsessEtlService, BsessEtlService>();

        services.AddHostedService<BsessImportJob>();

        return services;
    }
}
