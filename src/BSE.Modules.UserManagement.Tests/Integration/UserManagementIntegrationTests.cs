using System.Net;
using System.Security.Claims;
using BSE.Modules.UserManagement.Identity;
using BSE.Modules.UserManagement.Models;
using BSE.Modules.UserManagement.Repositories;
using BSE.Modules.UserManagement.Tests.TestAuth;
using BSE.SharedKernel;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace BSE.Modules.UserManagement.Tests.Integration;

/// <summary>
/// Integration tests for the User Management module identity pipeline.
/// Uses <see cref="TestAuthHandler"/> in place of a real IdP — no Azure AD calls made.
///
/// Claim format used:
///   <c>preferred_username</c> = "testuser@placeholder.domain"  (Azure AD OIDC format).
///
/// When real Azure AD is provisioned, ONLY environment variables change:
///   OIDC__Authority  = https://login.microsoftonline.com/{tenantId}/v2.0
///   OIDC__ClientId   = {appRegistrationClientId}
///   OIDC__ClientSecret = {clientSecret}
/// No code changes are required.
/// </summary>
[Trait("Category", "Integration")]
public sealed class UserManagementIntegrationTests : IClassFixture<UserManagementWebFactory>
{
    private readonly UserManagementWebFactory _factory;

    public UserManagementIntegrationTests(UserManagementWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthenticatedUser_ReceivesCorrectGroupClaim()
    {
        // Arrange: the factory's mock repository is configured to return DataEntry for testuser.
        const string upn = "testuser@placeholder.domain";
        var user = new User(1, "testuser", upn, "Test User", null, true, (int)UserGroup.DataEntry, UserGroup.DataEntry);
        _factory.MockUserRepository.GetByUpnAsync(upn).Returns(user);

        var client = _factory.CreateClient();

        // Act: call a probe endpoint that echoes the bse:group claim.
        var response = await client.GetAsync("/test/group-claim");

        // Assert: GroupClaimsTransformation resolved the correct group claim.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be(UserGroup.DataEntry.ToString());
    }

    [Fact]
    public async Task AuthenticatedUser_NtLoginFallback_ResolvesGroup()
    {
        // Arrange: UPN lookup fails, NTLogin fallback succeeds.
        const string upn = "bob.legacy@placeholder.domain";
        const string ntLogin = "bob.legacy";
        var user = new User(2, ntLogin, null, "Bob Legacy", null, true, (int)UserGroup.ReadOnly, UserGroup.ReadOnly);

        _factory.MockUserRepository.GetByUpnAsync(upn).Returns((User?)null);
        _factory.MockUserRepository.GetByNtLoginAsync(ntLogin).Returns(user);

        // Configure factory with this specific UPN for this test.
        var client = _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(s =>
                s.PostConfigure<TestAuthOptions>(opts =>
                    opts.DefaultUpn = upn)))
            .CreateClient();

        var response = await client.GetAsync("/test/group-claim");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be(UserGroup.ReadOnly.ToString());
    }
}

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that:
/// <list type="bullet">
///   <item>Replaces OIDC with <see cref="TestAuthHandler"/> (no real IdP)</item>
///   <item>Replaces <see cref="IUserRepository"/> with an NSubstitute mock (no real DB)</item>
///   <item>Registers a minimal probe route <c>GET /test/group-claim</c></item>
/// </list>
/// </summary>
public sealed class UserManagementWebFactory : WebApplicationFactory<Program>
{
    /// <summary>Access the mock repository to configure return values per test.</summary>
    public IUserRepository MockUserRepository { get; } = Substitute.For<IUserRepository>();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace OIDC authentication with test handler.
            services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<TestAuthOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            // Replace repository with mock — no database connection required.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IUserRepository));
            if (descriptor is not null) services.Remove(descriptor);
            services.AddScoped<IUserRepository>(_ => MockUserRepository);

            // Probe endpoint: returns the resolved bse:group claim value.
            services.AddRouting();
        });

        builder.ConfigureServices(services =>
        {
            services.AddTransient<IStartupFilter>(_ => new ProbeStartupFilter());
        });
    }

    /// <summary>Registers the /test/group-claim probe route after the host pipeline is built.</summary>
    private sealed class ProbeStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                next(app);
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/test/group-claim",
                        (HttpContext ctx) =>
                        {
                            var group = ctx.User.FindFirstValue(ClaimsUserContext.BseGroupClaimType)
                                        ?? string.Empty;
                            return Results.Text(group);
                        }).RequireAuthorization("Authenticated");
                });
            };
    }
}
