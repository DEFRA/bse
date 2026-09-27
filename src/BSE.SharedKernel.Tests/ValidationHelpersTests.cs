namespace BSE.SharedKernel.Tests;

public class ValidationHelpersTests
{
    [Theory]
    [InlineData("name@example.com")]
    [InlineData("first.last@sub.example.co.uk")]
    [InlineData("a@b.co")]
    public void IsValidEmail_ValidAddresses_ReturnsTrue(string email)
    {
        ValidationHelpers.IsValidEmail(email).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("@missing-local-part.com")]
    public void IsValidEmail_InvalidAddresses_ReturnsFalse(string? email)
    {
        ValidationHelpers.IsValidEmail(email!).Should().BeFalse();
    }

    [Fact]
    public void IsValidEmail_AddressWithDisplayName_ReturnsFalse()
    {
        // MailAddress parses "Name <addr@example.com>" successfully but the
        // parsed Address won't equal the raw input, so this should be rejected.
        ValidationHelpers.IsValidEmail("Display Name <name@example.com>").Should().BeFalse();
    }
}
