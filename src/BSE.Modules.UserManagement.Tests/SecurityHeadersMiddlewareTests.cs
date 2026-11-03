using BSE.Host.Helpers;
using BSE.Host.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BSE.Modules.UserManagement.Tests;

public class SecurityHeadersMiddlewareTests
{
    private static async Task<HttpClient> NewClientAsync(bool hsts)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(s => s.AddRouting())
                .Configure(app =>
                {
                    app.UseMiddleware<SecurityHeadersMiddleware>(hsts);
                    app.UseExceptionHandler(h => h.Run(ctx =>
                    {
                        ctx.Response.StatusCode = 500;
                        return Task.CompletedTask;
                    }));
                    app.Run(ctx => ctx.Request.Path.Value switch
                    {
                        "/redirect" => Task.Run(() => ctx.Response.Redirect("/elsewhere")),
                        "/boom" => throw new InvalidOperationException("boom"),
                        "/missing" => Task.Run(() => ctx.Response.StatusCode = 404),
                        _ => ctx.Response.WriteAsync("ok")
                    });
                }))
            .StartAsync();
        return host.GetTestClient();
    }

    [Theory]
    [InlineData("/", 200)]
    [InlineData("/redirect", 302)]
    [InlineData("/missing", 404)]
    [InlineData("/boom", 500)]
    public async Task EveryResponse_CarriesBaselineHeaders(string path, int status)
    {
        var client = await NewClientAsync(hsts: true);

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        ((int)response.StatusCode).Should().Be(status);
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("frame-ancestors 'none'");
        response.Headers.GetValues("X-Frame-Options").Single().Should().Be("DENY");
        response.Headers.GetValues("X-Content-Type-Options").Single().Should().Be("nosniff");
        response.Headers.GetValues("Referrer-Policy").Single().Should().NotBeNullOrEmpty();
        response.Headers.GetValues("Strict-Transport-Security").Single().Should().StartWith("max-age=");
    }

    [Fact]
    public async Task Csp_UsesPerRequestScriptNonce_WithoutUnsafeInlineScripts()
    {
        var client = await NewClientAsync(hsts: false);

        var first = (await client.GetAsync("/", TestContext.Current.CancellationToken)).Headers.GetValues("Content-Security-Policy").Single();
        var second = (await client.GetAsync("/", TestContext.Current.CancellationToken)).Headers.GetValues("Content-Security-Policy").Single();

        var scriptSrc = first.Split(';').Select(d => d.Trim()).Single(d => d.StartsWith("script-src ", StringComparison.Ordinal));
        scriptSrc.Should().MatchRegex("^script-src 'self' 'nonce-[A-Za-z0-9+/=]+'$");
        scriptSrc.Should().NotContain("unsafe-inline");
        second.Should().NotBe(first);
    }

    [Fact]
    public void NonceTagHelper_AddsNonce_OnlyWhenMissing()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[SecurityHeadersMiddleware.NonceItemKey] = "abc";
        var helper = new CspNonceTagHelper
        {
            ViewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext { HttpContext = httpContext }
        };

        var output = NewScriptOutput();
        helper.Process(NewContext(), output);
        output.Attributes["nonce"].Value.Should().Be("abc");

        var existing = NewScriptOutput();
        existing.Attributes.SetAttribute("nonce", "keep");
        helper.Process(NewContext(), existing);
        existing.Attributes["nonce"].Value.Should().Be("keep");

        httpContext.Items.Clear();
        var none = NewScriptOutput();
        helper.Process(NewContext(), none);
        none.Attributes.ContainsName("nonce").Should().BeFalse();
    }

    private static Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext NewContext() =>
        new([], new Dictionary<object, object>(), "id");

    private static Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput NewScriptOutput() =>
        new("script", [], (_, _) => Task.FromResult<Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContent>(
            new Microsoft.AspNetCore.Razor.TagHelpers.DefaultTagHelperContent()));

    [Fact]
    public async Task Hsts_IsOmitted_WhenDisabled()
    {
        var client = await NewClientAsync(hsts: false);

        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.Headers.Contains("Strict-Transport-Security").Should().BeFalse();
        response.Headers.Contains("X-Frame-Options").Should().BeTrue();
    }
}
