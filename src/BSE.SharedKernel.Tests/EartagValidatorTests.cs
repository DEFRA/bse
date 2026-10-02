namespace BSE.SharedKernel.Tests;

public class EartagValidatorTests
{
    // Crown Dependency alpha-numeric herds take a 1-5 digit animal number, and an
    // all-zero numerical part is never valid. Both rules were lost in a refactor.
    [Theory]
    [InlineData("GY1", "00000")]
    [InlineData("MN123", "00000")]
    [InlineData("JY1234", "00000")]
    public void Validate_RejectsAllZeroAnimalComponent(string herd, string animal)
    {
        EartagValidator.Validate("UK", herd, animal)
            .Should().Be("Animal component is invalid: The numerical part contains only zeros");
    }

    [Theory]
    [InlineData("GY1", "123456")]
    [InlineData("MN123", "123456")]
    [InlineData("JY1234", "123456")]
    public void Validate_RejectsAnimalComponentLongerThanFiveDigits(string herd, string animal)
    {
        EartagValidator.Validate("UK", herd, animal)
            .Should().Be("Animal component is invalid: It should consist of 1 to 5 numerical digits");
    }

    [Theory]
    [InlineData("GY1", "1")]
    [InlineData("MN123", "1")]
    [InlineData("JY1234", "1")]
    public void Validate_AcceptsSingleDigitAnimalComponent(string herd, string animal)
    {
        EartagValidator.Validate("UK", herd, animal).Should().BeNull();
    }

    [Theory]
    [InlineData("X0000")]
    [InlineData("00000")]
    public void Validate_RejectsAllZeroGbAlphaNumericAnimalComponent(string animal)
    {
        EartagValidator.Validate("UK", "AB12", animal)
            .Should().Be("Animal component is invalid: The numerical part contains only zeros");
    }
}
