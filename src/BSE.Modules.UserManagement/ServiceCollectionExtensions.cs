using BSE.Modules.UserManagement.Identity;
using BSE.Modules.UserManagement.Repositories;
using BSE.Modules.UserManagement.Services;
using BSE.SharedKernel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BSE.Modules.UserManagement;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all User Management module services into the DI container.
    /// Call from <c>BSE.Host/Program.cs</c>.
    /// </summary>
    public static IServiceCollection AddUserManagementModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        // IClaimsTransformation runs during authentication pipeline to add bse:group and role claims.
        services.AddScoped<IClaimsTransformation, GroupClaimsTransformation>();

        // IUserContext is resolved per-request from the current ClaimsPrincipal.
        services.AddScoped<IUserContext, ClaimsUserContext>();

        return services;
    }
}
