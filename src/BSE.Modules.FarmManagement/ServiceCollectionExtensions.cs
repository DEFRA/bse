using BSE.Modules.FarmManagement.Repositories;
using BSE.Modules.FarmManagement.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BSE.Modules.FarmManagement;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Farm Management module services into the DI container.
    /// Call from <c>BSE.Host/Program.cs</c>.
    /// </summary>
    public static IServiceCollection AddFarmManagementModule(this IServiceCollection services)
    {
        services.AddScoped<IFarmRepository, FarmRepository>();
        services.AddScoped<IFarmRelationRepository, FarmRelationRepository>();
        services.AddScoped<IHerdSizeRepository, HerdSizeRepository>();
        services.AddScoped<IVetnetRepository, VetnetRepository>();
        services.AddScoped<IFarmService, FarmService>();
        return services;
    }
}
