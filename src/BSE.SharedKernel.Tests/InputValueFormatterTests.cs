using BSE.SharedKernel;
using FluentAssertions;

namespace BSE.SharedKernel.Tests;

public sealed class InputValueFormatterTests
{
    [Fact]
    public void ToInputValue_WhenValueIsNull_ReturnsEmptyString()
    {
        InputValueFormatter.ToInputValue((int?)null).Should().Be(string.Empty);
    }

    [Fact]
    public void ToInputValue_WhenValueIsZero_PreservesZero()
    {
        InputValueFormatter.ToInputValue(0).Should().Be("0");
        InputValueFormatter.ToInputValue((int?)0).Should().Be("0");
    }
}
