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

        services.PostConfigure<AdnsMsGraphOptions>(options =>
        {
            options.MsGraphTenantId = FirstNonEmpty(
                options.MsGraphTenantId,
                configuration["AdnsMsGraph_MsGraphTenantId"]);

            options.MsGraphClientId = FirstNonEmpty(
                options.MsGraphClientId,
                configuration["AdnsMsGraph_MsGraphClientId"]);

            options.MsGraphClientSecret = FirstNonEmpty(
                options.MsGraphClientSecret,
                configuration["AdnsMsGraph_MsGraphClientSecret"],
                configuration["MsGraphClientSecret"]);

            options.MsGraphSenderUserId = FirstNonEmpty(
                options.MsGraphSenderUserId,
                configuration["AdnsMsGraph_MsGraphSenderUserId"]);

            options.FromAddress = FirstNonEmpty(
                options.FromAddress,
                configuration["AdnsMsGraph_FromAddress"]);

            options.ToAddress = FirstNonEmpty(
                options.ToAddress,
                configuration["AdnsMsGraph_ToAddress"]);
        });

        services.AddScoped<IAdnsRepository, AdnsRepository>();
        services.AddScoped<IAdnsExportService, AdnsExportService>();

        services.AddHttpClient<IMSGraphMailClient, MSGraphMailClient>();

        return services;

        static string FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;
    }
}
