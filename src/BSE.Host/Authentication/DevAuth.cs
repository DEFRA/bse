using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

// Dev-only OIDC bypass — this file is NOT committed to the repository.
// Drop it or exclude it from git; Program.cs compiles and runs OIDC-only without it.

internal static partial class DevAuth
{
    static partial void DoRegister(WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment()
            || !builder.Configuration.GetValue<bool>("Authentication:BypassEnabled"))
            return;

        var ntLogin = builder.Configuration["Authentication:DevUserNtLogin"] ?? "dev-user";

        builder.Services.AddAuthentication()
            .AddScheme<DevAuthOptions, DevAuthHandler>(
                DevAuthHandler.SchemeName, opts => opts.NtLogin = ntLogin);

        // Override OIDC defaults after they are registered.
        builder.Services.PostConfigure<AuthenticationOptions>(opts =>
        {
            opts.DefaultAuthenticateScheme = DevAuthHandler.SchemeName;
            opts.DefaultChallengeScheme    = DevAuthHandler.SchemeName;
            opts.DefaultScheme             = DevAuthHandler.SchemeName;
        });
    }
}

internal sealed class DevAuthOptions : AuthenticationSchemeOptions
{
    public string NtLogin { get; set; } = "dev-user";
}

internal sealed class DevAuthHandler(
    IOptionsMonitor<DevAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<DevAuthOptions>(options, logger, encoder)
{
    internal const string SchemeName = "Development";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("preferred_username", Options.NtLogin),
            new Claim(ClaimTypes.Name, Options.NtLogin),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
