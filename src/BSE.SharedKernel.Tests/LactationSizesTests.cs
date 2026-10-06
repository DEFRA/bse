namespace BSE.SharedKernel.Tests;

public sealed class LactationSizesTests
{
    private sealed class TestLactationSizes : ILactationSizes
    {
        public int? Lactation1Size { get; set; }
        public int? Lactation2Size { get; set; }
        public int? Lactation3Size { get; set; }
        public int? Lactation4Size { get; set; }
        public int? Lactation5Size { get; set; }
        public int? Lactation6Size { get; set; }
        public int? Lactation7Size { get; set; }
        public int? Lactation8Size { get; set; }
        public int? Lactation9Size { get; set; }
        public int? Lactation10Size { get; set; }
        public int? Lactation10PlusSize { get; set; }
    }

    [Fact]
    public void All_EnumeratesValuesInOrder()
    {
        var row = new TestLactationSizes
        {
            Lactation1Size = 1,
            Lactation2Size = 2,
            Lactation3Size = 3,
            Lactation4Size = 4,
            Lactation5Size = 5,
            Lactation6Size = 6,
            Lactation7Size = 7,
            Lactation8Size = 8,
            Lactation9Size = 9,
            Lactation10Size = 10,
            Lactation10PlusSize = 11
        };

        row.All().Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
    }

    [Fact]
    public void Total_SumsOnlyNonNullValues()
    {
        var row = new TestLactationSizes
        {
            Lactation1Size = 5,
            Lactation2Size = null,
            Lactation3Size = 7,
            Lactation4Size = 0,
            Lactation5Size = 8,
            Lactation6Size = null,
            Lactation7Size = 11,
            Lactation8Size = null,
            Lactation9Size = 4,
            Lactation10Size = 3,
            Lactation10PlusSize = 2
        };

        row.Total().Should().Be(40);
    }

    [Fact]
    public void HasAnyValue_ReturnsTrue_WhenAtLeastOneEntryHasValue()
    {
        var row = new TestLactationSizes { Lactation5Size = 42 };

        row.HasAnyValue().Should().BeTrue();
    }

    [Fact]
    public void HasAnyValue_ReturnsFalse_WhenAllEntriesAreNull()
    {
        var row = new TestLactationSizes();

        row.HasAnyValue().Should().BeFalse();
    }

    [Fact]
    public void Normalize_FillsMissingValues_WithZero_WhenAnyValueExists()
    {
        var row = new TestLactationSizes { Lactation3Size = 12 };

        row.Normalize();

        row.Lactation1Size.Should().Be(0);
        row.Lactation2Size.Should().Be(0);
        row.Lactation3Size.Should().Be(12);
        row.Lactation4Size.Should().Be(0);
        row.Lactation5Size.Should().Be(0);
        row.Lactation6Size.Should().Be(0);
        row.Lactation7Size.Should().Be(0);
        row.Lactation8Size.Should().Be(0);
        row.Lactation9Size.Should().Be(0);
        row.Lactation10Size.Should().Be(0);
        row.Lactation10PlusSize.Should().Be(0);
    }

    [Fact]
    public void Normalize_LeavesAllEntriesNull_WhenNoValueExists()
    {
        var row = new TestLactationSizes();

        row.Normalize();

        row.All().Should().OnlyContain(v => v == null);
    }
}
