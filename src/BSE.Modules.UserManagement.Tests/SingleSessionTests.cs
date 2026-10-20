using System.Security.Claims;
using BSE.Host.Authentication;
using BSE.Modules.UserManagement.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public class SingleSessionTests
{
    private static SignInSessionRegistry NewRegistry(out IDistributedCache cache)
    {
        cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new SignInSessionRegistry(cache);
    }

    [Fact]
    public async Task NewSignIn_MakesEarlierSessionNotCurrent()
    {
        var registry = NewRegistry(out _);

        var first = await registry.StartAsync("User@Defra.gov.uk");
        var second = await registry.StartAsync("user@defra.gov.uk");

        (await registry.IsCurrentAsync("user@defra.gov.uk", first)).Should().BeFalse();
        (await registry.IsCurrentAsync("user@defra.gov.uk", second)).Should().BeTrue();
    }

    [Fact]
    public async Task SignIn_ByAnotherUser_DoesNotAffectSession()
    {
        var registry = NewRegistry(out _);

        var mine = await registry.StartAsync("a@defra.gov.uk");
        await registry.StartAsync("b@defra.gov.uk");

        (await registry.IsCurrentAsync("a@defra.gov.uk", mine)).Should().BeTrue();
    }

    [Fact]
    public async Task MissingEntry_AdoptsCallerSessionRatherThanSigningUserOut()
    {
        var registry = NewRegistry(out _);

        (await registry.IsCurrentAsync("a@defra.gov.uk", "abc")).Should().BeTrue();
        (await registry.IsCurrentAsync("a@defra.gov.uk", "other")).Should().BeFalse();
    }

    [Fact]
    public async Task OnSigningIn_AddsSessionIdClaim_AndKeepsExistingOne()
    {
        var registry = NewRegistry(out _);
        var context = NewSigningInContext(registry, Principal(email: "a@defra.gov.uk"));

        await SingleSessionCookieEvents.OnSigningIn(context);

        var sessionId = context.Principal!.FindFirstValue(SingleSessionCookieEvents.SessionIdClaimType);
        sessionId.Should().NotBeNullOrEmpty();
        (await registry.IsCurrentAsync("a@defra.gov.uk", sessionId!)).Should().BeTrue();

        // A sliding-expiration renewal re-signs the same principal and must not start a new session.
        await SingleSessionCookieEvents.OnSigningIn(context);
        context.Principal.FindFirstValue(SingleSessionCookieEvents.SessionIdClaimType).Should().Be(sessionId);
    }

    [Fact]
    public async Task OnValidatePrincipal_AcceptsLatestSession()
    {
        var registry = NewRegistry(out _);
        var id = await registry.StartAsync("a@defra.gov.uk");
        var context = NewValidateContext(registry, Principal("a@defra.gov.uk", id), out var auth);

        await SingleSessionCookieEvents.OnValidatePrincipal(context);

        context.Principal.Should().NotBeNull();
        await auth.DidNotReceiveWithAnyArgs().SignOutAsync(default!, default, default);
    }

    [Fact]
    public async Task OnValidatePrincipal_RejectsSupersededSession()
    {
        var registry = NewRegistry(out _);
        var old = await registry.StartAsync("a@defra.gov.uk");
        await registry.StartAsync("a@defra.gov.uk");
        var context = NewValidateContext(registry, Principal("a@defra.gov.uk", old), out var auth);

        await SingleSessionCookieEvents.OnValidatePrincipal(context);

        context.Principal.Should().BeNull();
        await auth.Received(1).SignOutAsync(Arg.Any<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, Arg.Any<AuthenticationProperties?>());
    }

    [Fact]
    public async Task OnValidatePrincipal_RejectsCookieWithoutSessionId()
    {
        var registry = NewRegistry(out _);
        var context = NewValidateContext(registry, Principal("a@defra.gov.uk"), out _);

        await SingleSessionCookieEvents.OnValidatePrincipal(context);

        context.Principal.Should().BeNull();
    }

    private static ClaimsPrincipal Principal(string email, string? sessionId = null)
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimsUserContext.EmailClaimType, email));
        if (sessionId is not null)
            identity.AddClaim(new Claim(SingleSessionCookieEvents.SessionIdClaimType, sessionId));
        return new ClaimsPrincipal(identity);
    }

    private static DefaultHttpContext NewHttpContext(ISignInSessionRegistry registry, IAuthenticationService? auth = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(registry);
        services.AddSingleton(auth ?? Substitute.For<IAuthenticationService>());
        return new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
    }

    private static CookieSigningInContext NewSigningInContext(ISignInSessionRegistry registry, ClaimsPrincipal principal)
    {
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));
        return new CookieSigningInContext(
            NewHttpContext(registry), scheme, new CookieAuthenticationOptions(), principal,
            new AuthenticationProperties(), new CookieOptions());
    }

    private static CookieValidatePrincipalContext NewValidateContext(
        ISignInSessionRegistry registry, ClaimsPrincipal principal, out IAuthenticationService auth)
    {
        auth = Substitute.For<IAuthenticationService>();
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme);
        return new CookieValidatePrincipalContext(NewHttpContext(registry, auth), scheme, new CookieAuthenticationOptions(), ticket);
    }
}
