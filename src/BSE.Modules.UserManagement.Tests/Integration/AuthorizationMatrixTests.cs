using System.Net;
using System.Security.Claims;
using BSE.Modules.CaseManagement.Services;
using BSE.Modules.FarmManagement.Models;
using BSE.Modules.FarmManagement.Services;
using BSE.Modules.ReferenceData.Models;
using BSE.Modules.ReferenceData.Services;
using BSE.Modules.UserManagement.Identity;
using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Repositories;
using BSE.Modules.UserManagement.Services;
using BSE.Modules.UserManagement.Tests.TestAuth;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests.Integration;

/// <summary>
/// AUTH-04 / AUTH-05: data-driven role x page authorization matrix.
///
/// Each (role, page) pair is an individually reported xunit case. The assertion is about the
/// AUTHORIZATION outcome only — a 404 or validation page still proves authorization passed.
///
/// Mechanism: <see cref="MatrixClaimsTransformation"/> replaces the production
/// <c>GroupClaimsTransformation</c> for this factory only, and reads the role set from the
/// <c>X-Test-Roles</c> request header. This is the least invasive way to vary roles per request
/// (the shared <c>TestAuthHandler</c> options are fixed per scheme, so they cannot vary per test).
/// It isolates the Program.cs policy definitions + page [Authorize] attributes, which is exactly
/// what AUTH-04/05 target; the DB group -> policy mapping is covered separately by
/// <c>GroupMatrixTests</c> below and by GroupClaimsTransformationTests.
/// </summary>
[Trait("Category", "Integration")]
[Collection(ProgramHostCollection.Name)]
public sealed class AuthorizationMatrixTests : IClassFixture<AuthorizationMatrixWebFactory>
{
    private readonly AuthorizationMatrixWebFactory _factory;

    public AuthorizationMatrixTests(AuthorizationMatrixWebFactory factory) => _factory = factory;

    public const string NoRole = "(none)";

    /// <summary>Representative page per policy in Program.cs. Pages chosen to need as little data as possible.</summary>
    public static readonly (string Url, string Policy, string[] AllowedRoles)[] ProtectedPages =
    [
        ("/Search/Menu",                     "Authenticated",         ["*"]),
        ("/Help",                            "Authenticated",         ["*"]),
        ("/Farm/Edit/99999999999",          "DataEntry",             ["DataEntry"]),
        ("/AdnsExport/Menu",                 "DEFRAMaintenance",      ["DEFRAMaintenance"]),
        ("/Case/MoveCase?rbse=R/999/99",     "DEFRAOrVLAMaintenance", ["DEFRAMaintenance", "VLAMaintenance"]),
        ("/OssExport/Menu",                  "VLAAccess",             ["VLAAccess"]),
        ("/CaseWork/Menu",                   "VLAMaintenance",        ["VLAMaintenance"]),
        ("/Admin/Users",                     "VLAMaintenance",        ["VLAMaintenance"]),
        ("/Admin/PickLists",                 "PickListAccess",        ["PickListAccess"]),
        ("/Farm/Lookup",                     "FarmCreation",          ["FarmCreation"]),
        ("/AuditLog/Menu",                   "AuditAccess",           ["DEFRAAccess", "VLAAccess"]),
        ("/Bsess/Menu",                      "AuditAccess",           ["DEFRAAccess", "VLAAccess"]),
    ];

    public static readonly string[] Roles =
    [
        "ReadOnly", "DataEntry", "DEFRAAccess", "VLAAccess",
        "DEFRAMaintenance", "VLAMaintenance", "PickListAccess", "FarmCreation", NoRole
    ];

    public static TheoryData<string, string, string, bool> Matrix()
    {
        var data = new TheoryData<string, string, string, bool>();
        foreach (var (url, policy, allowed) in ProtectedPages)
        {
            foreach (var role in Roles)
            {
                var expectAllowed = allowed.Contains("*") || allowed.Contains(role);
                data.Add(url, policy, role, expectAllowed);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task Page_EnforcesPolicy_ForRole(string url, string policy, string role, bool expectAllowed)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (role != NoRole)
            client.DefaultRequestHeaders.Add(MatrixClaimsTransformation.RolesHeader, role);

        var response = await client.GetAsync(url);

        var location = response.Headers.Location?.ToString() ?? string.Empty;
        var deniedRedirect = response.StatusCode is HttpStatusCode.Found or HttpStatusCode.Redirect
                             && location.StartsWith("/Unauthorized", StringComparison.OrdinalIgnoreCase);

        // AUTH-05 hard rule: an authorization decision must never surface as a server error.
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "policy {0} on {1} for role {2} must not produce a 500", policy, url, role);

        if (expectAllowed)
        {
            response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
                "role {0} satisfies policy {1} on {2}", role, policy, url);
            deniedRedirect.Should().BeFalse(
                "role {0} satisfies policy {1} on {2} but was redirected to {3}", role, policy, url, location);
        }
        else
        {
            (response.StatusCode == HttpStatusCode.Forbidden || deniedRedirect).Should().BeTrue(
                "role {0} must NOT satisfy policy {1} on {2}; got {3} {4}",
                role, policy, url, (int)response.StatusCode, location);
        }
    }
}

/// <summary>
/// AUTH-02 / AUTH-03 and the DB-group -> policy mapping, exercised through the REAL
/// <c>GroupClaimsTransformation</c>. The identity is selected per request via the
/// <c>X-Test-Upn</c> header, which the mocked <see cref="IUserRepository"/> resolves.
/// </summary>
[Trait("Category", "Integration")]
[Collection(ProgramHostCollection.Name)]
public sealed class GroupMatrixTests : IClassFixture<GroupMatrixWebFactory>
{
    private readonly GroupMatrixWebFactory _factory;

    public GroupMatrixTests(GroupMatrixWebFactory factory) => _factory = factory;

    public static TheoryData<string, string, bool> GroupMatrix()
    {
        // Group -> policies, per GroupClaimsTransformation.GetPoliciesForGroup.
        var groups = new (string Group, string[] Policies)[]
        {
            ("DEFRA Viewer",            ["ReadOnly", "DEFRAAccess"]),
            ("DEFRA Data Entry",        ["ReadOnly", "DataEntry", "FarmCreation", "DEFRAAccess"]),
            ("DEFRA Maintenance",       ["ReadOnly", "DataEntry", "DEFRAMaintenance", "PickListAccess", "FarmCreation", "DEFRAAccess"]),
            ("VLA Data Entry",          ["ReadOnly", "DataEntry", "VLAAccess", "PickListAccess"]),
            ("VLA Maintenance",         ["ReadOnly", "DataEntry", "DEFRAMaintenance", "VLAAccess", "VLAMaintenance", "PickListAccess", "FarmCreation"]),
            ("DEFRA AI Wales Scotland", ["ReadOnly"]),
            ("DEFRA AHO User",          ["ReadOnly"]),
        };

        var data = new TheoryData<string, string, bool>();
        foreach (var (group, policies) in groups)
        {
            foreach (var (url, policy, allowedRoles) in AuthorizationMatrixTests.ProtectedPages)
            {
                var expectAllowed = allowedRoles.Contains("*") || allowedRoles.Any(policies.Contains);
                data.Add(group, url, expectAllowed);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(GroupMatrix))]
    public async Task Page_EnforcesPolicy_ForDatabaseGroup(string group, string url, bool expectAllowed)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(HeaderUpnAuthHandler.UpnHeader, GroupMatrixWebFactory.UpnFor(group));

        var response = await client.GetAsync(url);

        var location = response.Headers.Location?.ToString() ?? string.Empty;
        var denied = response.StatusCode == HttpStatusCode.Forbidden
                     || (response.StatusCode is HttpStatusCode.Found or HttpStatusCode.Redirect
                         && location.StartsWith("/Unauthorized", StringComparison.OrdinalIgnoreCase));

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        denied.Should().Be(!expectAllowed,
            "group '{0}' on {1}: expected allowed={2}, got {3} {4}", group, url, expectAllowed, (int)response.StatusCode, location);
    }

    [Fact]
    public async Task UserAbsentFromUserTable_IsRedirectedToUnauthorized()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(HeaderUpnAuthHandler.UpnHeader, "ghost@placeholder.domain");

        var response = await client.GetAsync("/Search/Menu");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.ToString().Should().StartWith("/Unauthorized");
    }

    [Fact]
    public async Task UnauthorizedPage_IsReachable_WithoutAnyGroup()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(HeaderUpnAuthHandler.UpnHeader, "ghost@placeholder.domain");

        var response = await client.GetAsync("/Unauthorized");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

/// <summary>Issues an authenticated principal whose <c>preferred_username</c> comes from a request header.</summary>
public sealed class HeaderUpnAuthHandler : AuthenticationHandler<TestAuthOptions>
{
    public const string SchemeName = "HeaderUpnAuth";
    public const string UpnHeader = "X-Test-Upn";

    public HeaderUpnAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<TestAuthOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var upn = Request.Headers[UpnHeader].ToString();
        if (string.IsNullOrWhiteSpace(upn)) upn = Options.DefaultUpn;

        var identity = new ClaimsIdentity(
            [new Claim("preferred_username", upn), new Claim(ClaimTypes.Name, upn)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

/// <summary>Replaces GroupClaimsTransformation: emits role claims from the <c>X-Test-Roles</c> header.</summary>
public sealed class MatrixClaimsTransformation : Microsoft.AspNetCore.Authentication.IClaimsTransformation
{
    public const string RolesHeader = "X-Test-Roles";

    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _accessor;

    public MatrixClaimsTransformation(Microsoft.AspNetCore.Http.IHttpContextAccessor accessor) => _accessor = accessor;

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return Task.FromResult(principal);

        if (principal.HasClaim(c => c.Type == ClaimsUserContext.BseGroupIdClaimType))
            return Task.FromResult(principal);

        var clone = principal.Clone();
        var identity = (ClaimsIdentity)clone.Identity!;

        foreach (var c in identity.FindAll(ClaimTypes.Role).ToList())
            identity.RemoveClaim(c);

        // Every test principal is a known user, so it always carries a group id — otherwise the
        // Program.cs "no group" middleware would short-circuit before authorization runs.
        identity.AddClaim(new Claim(ClaimsUserContext.BseGroupIdClaimType, "99"));
        identity.AddClaim(new Claim(ClaimsUserContext.BseGroupClaimType, "Test Group"));

        var header = _accessor.HttpContext?.Request.Headers[RolesHeader].ToString() ?? string.Empty;
        foreach (var role in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        return Task.FromResult(clone);
    }
}

/// <summary>Shared service stubs so no database is required by either matrix factory.</summary>
internal static class MatrixServiceStubs
{
    public static void Apply(IServiceCollection services)
    {
        var farm = Substitute.For<IFarmService>();
        farm.GetByCphhAsync(Arg.Any<string>()).Returns((FarmRecord?)null);
        services.RemoveAll<IFarmService>();
        services.AddScoped(_ => farm);

        var cases = Substitute.For<ICaseService>();
        services.RemoveAll<ICaseService>();
        services.AddScoped(_ => cases);

        var lookups = Substitute.For<ILookupDataService>();
        lookups.GetUserGroupsAsync().Returns(new List<LuUserGroup>().AsEnumerable());
        services.RemoveAll<ILookupDataService>();
        services.AddScoped(_ => lookups);

        var admin = Substitute.For<IEditableLookupAdminService>();
        services.RemoveAll<IEditableLookupAdminService>();
        services.AddScoped(_ => admin);

        var users = Substitute.For<IUserManagementService>();
        users.GetAllUsersAsync().Returns(new List<User>().AsEnumerable());
        services.RemoveAll<IUserManagementService>();
        services.AddScoped(_ => users);
    }
}

public sealed class AuthorizationMatrixWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Authentication:BypassEnabled", "false");
        builder.UseSetting("Authentication:UseWindowsIdentity", "false");
        builder.UseSetting("Authentication:DevUserEmail", "matrix@placeholder.domain");

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<TestAuthOptions, TestAuthHandler>(TestAuthHandler.SchemeName, options =>
            {
                options.DefaultUpn = "matrix@placeholder.domain";
                options.DefaultDisplayName = "Matrix Test";
            });

            services.AddHttpContextAccessor();
            services.RemoveAll<Microsoft.AspNetCore.Authentication.IClaimsTransformation>();
            services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, MatrixClaimsTransformation>();

            services.RemoveAll<IUserRepository>();
            services.AddScoped(_ => Substitute.For<IUserRepository>());

            MatrixServiceStubs.Apply(services);
        });
    }
}

public sealed class GroupMatrixWebFactory : WebApplicationFactory<Program>
{
    public static string UpnFor(string group) =>
        group.Replace(' ', '.').ToLowerInvariant() + "@placeholder.domain";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Authentication:BypassEnabled", "false");
        builder.UseSetting("Authentication:UseWindowsIdentity", "false");
        builder.UseSetting("Authentication:DevUserEmail", "groupmatrix@placeholder.domain");

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = HeaderUpnAuthHandler.SchemeName;
                options.DefaultChallengeScheme = HeaderUpnAuthHandler.SchemeName;
                options.DefaultScheme = HeaderUpnAuthHandler.SchemeName;
            })
            .AddScheme<TestAuthOptions, HeaderUpnAuthHandler>(HeaderUpnAuthHandler.SchemeName, _ => { });

            var repo = Substitute.For<IUserRepository>();
            repo.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);

            var groups = new[]
            {
                "DEFRA Viewer", "DEFRA Data Entry", "DEFRA Maintenance",
                "VLA Data Entry", "VLA Maintenance", "DEFRA AI Wales Scotland", "DEFRA AHO User"
            };
            var id = 1;
            foreach (var group in groups)
            {
                var upn = UpnFor(group);
                repo.GetByEmailAsync(upn).Returns(new User(
                    id, $"user{id}", upn, group, upn, true, id, UserGroup.None, group));
                id++;
            }

            services.RemoveAll<IUserRepository>();
            services.AddScoped(_ => repo);

            MatrixServiceStubs.Apply(services);
        });
    }
}
