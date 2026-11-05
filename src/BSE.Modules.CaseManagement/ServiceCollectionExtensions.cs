using BSE.Infrastructure;
using BSE.Modules.Batch.Repositories;
using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.CaseWork.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace BSE.Modules.CaseManagement;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCaseManagementModule(this IServiceCollection services)
    {
        services.AddScoped<ICaseRepository, CaseRepository>();
        services.AddScoped<IClinicalRepository, ClinicalRepository>();
        services.AddScoped<IBabRepository, BabRepository>();
        services.AddScoped<IFeedRepository, FeedRepository>();
        services.AddScoped<ITestRepository, TestRepository>();
        services.AddScoped<IOtherOwnerRepository, OtherOwnerRepository>();
        services.AddScoped<IPedigreeRepository, PedigreeRepository>();
        services.AddScoped(sp => new CaseServiceDependencies(
            sp.GetRequiredService<IDbConnectionFactory>(),
            sp.GetRequiredService<ICaseRepository>(),
            sp.GetRequiredService<IClinicalRepository>(),
            sp.GetRequiredService<IBabRepository>(),
            sp.GetRequiredService<IFeedRepository>(),
            sp.GetRequiredService<ITestRepository>(),
            sp.GetRequiredService<IOtherOwnerRepository>(),
            sp.GetRequiredService<IPedigreeRepository>(),
            sp.GetRequiredService<IBatchRepository>(),
            sp.GetRequiredService<ICaseWorkRepository>()));
        services.AddScoped<ICaseService, CaseService>();
        return services;
    }
}
