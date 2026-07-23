using BSE.Modules.CaseWork.Repositories;
using BSE.Modules.CaseWork.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BSE.Modules.CaseWork;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCaseWorkModule(this IServiceCollection services)
    {
        services.AddScoped<ICaseWorkRepository, CaseWorkRepository>();
        services.AddScoped<ICaseWorkService, CaseWorkService>();
        return services;
    }
}
