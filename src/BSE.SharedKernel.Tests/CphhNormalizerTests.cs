namespace BSE.SharedKernel.Tests;

public class CphhNormalizerTests
{
    [Theory]
    [InlineData("12/345/6789/01", "12345678901")]
    [InlineData("12345678901", "12345678901")]
    [InlineData("12-345-6789-01", "12345678901")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    [InlineData("ABC123", "123")]
    public void Normalize_ReturnsDigitsOnly(string? input, string expected)
    {
        CphhNormalizer.Normalize(input).Should().Be(expected);
    }
}
