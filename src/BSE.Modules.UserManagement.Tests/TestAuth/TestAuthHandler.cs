using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BSE.Modules.UserManagement.Tests.TestAuth;

/// <summary>
/// Test-only authentication handler that issues configurable claims without a real IdP.
/// Claim shape matches Azure AD OIDC output:
///   - <c>preferred_username</c> = UPN  (e.g. "testuser@placeholder.domain")
///   - <c>ClaimTypes.Name</c>           (display name)
///   - <c>bse:group</c>                 (populated by <c>GroupClaimsTransformation</c>)
///
/// Usage in integration tests: register via
///   <c>services.AddAuthentication(TestAuthHandler.SchemeName).AddScheme&lt;TestAuthOptions, TestAuthHandler&gt;(...)</c>
///
/// NOTE: When real Azure AD is wired (OIDC__* env vars), this handler is test-only and
/// never runs in production. Only environment variables change — zero code changes required.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<TestAuthOptions>
{
    public const string SchemeName = "TestAuth";

    public TestAuthHandler(
        IOptionsMonitor<TestAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = Options.Claims ?? [];

        // Ensure preferred_username is present (Azure AD UPN format).
        if (!claims.Any(c => c.Type == "preferred_username"))
        {
            claims = [.. claims, new Claim("preferred_username", Options.DefaultUpn)];
        }

        // Ensure display name is present.
        if (!claims.Any(c => c.Type == ClaimTypes.Name))
        {
            claims = [.. claims, new Claim(ClaimTypes.Name, Options.DefaultDisplayName)];
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class TestAuthOptions : AuthenticationSchemeOptions
{
    /// <summary>UPN issued as <c>preferred_username</c> claim when no explicit claims list is provided.</summary>
    public string DefaultUpn { get; set; } = "testuser@placeholder.domain";

    /// <summary>Display name issued as <c>ClaimTypes.Name</c> when no explicit claims list is provided.</summary>
    public string DefaultDisplayName { get; set; } = "Test User";

    /// <summary>
    /// Full set of claims to issue. If null, defaults containing <see cref="DefaultUpn"/> and
    /// <see cref="DefaultDisplayName"/> are added automatically.
    /// </summary>
    public IEnumerable<Claim>? Claims { get; set; }
}
