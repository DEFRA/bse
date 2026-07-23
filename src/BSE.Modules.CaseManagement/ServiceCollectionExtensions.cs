using BSE.Modules.CaseManagement.Repositories;
using BSE.Modules.CaseManagement.Services;
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
        services.AddScoped<ICaseService, CaseService>();
        return services;
    }
}
