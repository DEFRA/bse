using BSE.Modules.ReferenceData.Repositories;
using BSE.Modules.ReferenceData.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BSE.Modules.ReferenceData;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Reference Data module services into the DI container.
    /// Call from <c>BSE.Host/Program.cs</c>.
    /// </summary>
    public static IServiceCollection AddReferenceDataModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<ILookupDataService, LookupDataService>();
        services.AddScoped<IGeoLookupService, GeoLookupService>();
        services.AddScoped<IEditableLookupAdminService, EditableLookupAdminService>();
        return services;
    }
}
