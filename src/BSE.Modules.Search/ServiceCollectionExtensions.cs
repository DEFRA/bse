using BSE.Modules.Search.Repositories;
using BSE.Modules.Search.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BSE.Modules.Search;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ISearchRepository, SearchRepository>();
        services.AddScoped<ICaseSearchService, CaseSearchService>();
        services.AddScoped<IFarmSearchService, FarmSearchService>();
        services.AddScoped<IOutstandingDataSearchService, OutstandingDataSearchService>();
        return services;
    }
}
