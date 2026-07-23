using BSE.Modules.AnimalRelations.Repositories;
using BSE.Modules.AnimalRelations.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BSE.Modules.AnimalRelations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnimalRelationsModule(this IServiceCollection services)
    {
        services.AddScoped<IAnimalRelationsRepository, AnimalRelationsRepository>();
        services.AddScoped<IAnimalRelationsService, AnimalRelationsService>();
        return services;
    }
}
