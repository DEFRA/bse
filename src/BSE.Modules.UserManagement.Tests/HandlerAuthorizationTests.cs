using System.Reflection;
using System.Security.Claims;
using BSE.Host.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public class HandlerAuthorizationTests
{
    [Fact]
    public void EveryPostHandler_OnPagesWithBareAuthorize_DeclaresItsAuthorizationExplicitly()
    {
        var undeclared = typeof(HandlerAuthorizationPageFilter).Assembly.GetTypes()
            .Where(t => typeof(PageModel).IsAssignableFrom(t) && !t.IsAbstract)
            .Where(t => t.GetCustomAttributes<AuthorizeAttribute>().Any(a => a.Policy is null && a.Roles is null))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.Name.StartsWith("OnPost", StringComparison.Ordinal)
                            || m.Name.StartsWith("OnPut", StringComparison.Ordinal)
                            || m.Name.StartsWith("OnDelete", StringComparison.Ordinal))
                .Where(m => !m.GetCustomAttributes().OfType<IAuthorizeData>().Any())
                .Select(m => $"{t.Name}.{m.Name}"))
            .ToList();

        undeclared.Should().BeEmpty("a bare class-level [Authorize] admits any signed-in user, so each POST handler must state a policy or an explicit [Authorize]");
    }

    [Theory]
    [InlineData("DataEntry", true)]
    [InlineData("ReadOnly", false)]
    public async Task Filter_ForbidsHandlerWhenUserLacksPolicyRole(string role, bool allowed)
    {
        var result = await RunFilterAsync(nameof(Annotated.OnPostEdit), role);

        (result is null).Should().Be(allowed);
        if (!allowed)
            result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Filter_RequiresBothRoles_ForDataEntryVlaPolicy()
    {
        (await RunFilterAsync(nameof(Annotated.OnPostBoth), "DataEntry")).Should().BeOfType<ForbidResult>();
        (await RunFilterAsync(nameof(Annotated.OnPostBoth), "DataEntry", "VLAAccess")).Should().BeNull();
    }

    [Fact]
    public async Task Filter_IgnoresHandlersWithoutAuthorizeData()
    {
        (await RunFilterAsync(nameof(Annotated.OnPostOpen), "ReadOnly")).Should().BeNull();
    }

    private static async Task<IActionResult?> RunFilterAsync(string handlerName, params string[] roles)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder()
            .AddPolicy("DataEntry", p => p.RequireRole("DataEntry"))
            .AddPolicy("DataEntryVla", p => p.RequireRole("DataEntry").RequireRole("VLAAccess"));
        var provider = services.BuildServiceProvider();

        var identity = new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "test");
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
            RequestServices = provider
        };

        var handler = new Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure.HandlerMethodDescriptor
        {
            MethodInfo = typeof(Annotated).GetMethod(handlerName)!
        };
        var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(
            httpContext, new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor());
        var pageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext(actionContext);
        var context = new Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext(
            pageContext, [], handler, new Dictionary<string, object?>(), new Annotated());

        var filter = new HandlerAuthorizationPageFilter(
            provider.GetRequiredService<IAuthorizationPolicyProvider>(),
            provider.GetRequiredService<IAuthorizationService>());
        await filter.OnPageHandlerExecutionAsync(context, () => Task.FromResult(
            new Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutedContext(pageContext, [], handler, new Annotated())));

        return context.Result;
    }

    private sealed class Annotated : PageModel
    {
        [Authorize(Policy = "DataEntry")]
        public IActionResult OnPostEdit() => Page();

        [Authorize(Policy = "DataEntryVla")]
        public IActionResult OnPostBoth() => Page();

        public IActionResult OnPostOpen() => Page();
    }
}
