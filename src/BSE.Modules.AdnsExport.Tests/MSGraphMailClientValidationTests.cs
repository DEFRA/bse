using BSE.Modules.AdnsExport.Configuration;
using BSE.Modules.AdnsExport.Email;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BSE.Modules.AdnsExport.Tests;

public sealed class MSGraphMailClientValidationTests
{
    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("no network call expected");
    }

    private static MSGraphMailClient Create(AdnsMsGraphOptions options) =>
        new(new HttpClient(new FailingHandler()), Options.Create(options));

    private static AdnsMsGraphOptions Complete() => new()
    {
        MsGraphTenantId = "t", MsGraphClientId = "c", MsGraphClientSecret = "s", FromAddress = "from@example.test"
    };

    [Fact]
    public async Task SendAsync_ListsEveryMissingConfigurationValue()
    {
        var sut = Create(new AdnsMsGraphOptions());

        var act = () => sut.SendAsync("from@example.test", "to@example.test", "s", "b");

        var ex = (await act.Should().ThrowAsync<InvalidOperationException>()).Which;
        ex.Message.Should().Contain("MsGraphTenantId").And.Contain("MsGraphClientId")
            .And.Contain("MsGraphClientSecret").And.Contain("FromAddress");
    }

    [Theory]
    [InlineData("", "to@example.test", "AdnsMsGraph:FromAddress")]
    [InlineData("  ", "to@example.test", "AdnsMsGraph:FromAddress")]
    [InlineData("from@example.test", "", "recipient cannot be empty")]
    [InlineData("from@example.test", "  ", "recipient cannot be empty")]
    public async Task SendAsync_RejectsBlankAddresses(string from, string to, string expected)
    {
        var sut = Create(Complete());

        var act = () => sut.SendAsync(from, to, "s", "b");

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain(expected);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var act = () => Create(Complete()).Dispose();
        act.Should().NotThrow();
    }
}
