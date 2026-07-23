using BSE.Modules.Batch.Repositories;
using BSE.Modules.Batch.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BSE.Modules.Batch;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBatchModule(this IServiceCollection services)
    {
        services.AddScoped<IBatchRepository, BatchRepository>();
        services.AddScoped<IBatchService, BatchService>();
        return services;
    }
}
