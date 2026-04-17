using TrafficMonitor.Domain.ValueObjects;

namespace TrafficMonitor.Tests.Domain;

public class DetectionTests
{
    private static readonly BoundingBox ValidBoundingBox = new(0.1, 0.2, 0.3, 0.4);

    [Fact]
    public void Constructor_WithValidValues_Succeeds()
    {
        var detection = new Detection("car", 0.85, ValidBoundingBox);

        Assert.Equal("car", detection.Label);
        Assert.Equal(0.85, detection.Confidence);
        Assert.Equal(ValidBoundingBox, detection.BoundingBox);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidLabel_ThrowsArgumentException(string? label)
    {
        Assert.Throws<ArgumentException>(() => new Detection(label!, 0.5, ValidBoundingBox));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Constructor_WithConfidenceOutOfRange_ThrowsArgumentException(double confidence)
    {
        Assert.Throws<ArgumentException>(() => new Detection("car", confidence, ValidBoundingBox));
    }

    [Fact]
    public void Constructor_WithLabelContainingWhitespace_StoresTrimmedLabel()
    {
        var detection = new Detection("  car  ", 0.5, ValidBoundingBox);

        Assert.Equal("car", detection.Label);
    }

    [Fact]
    public void Constructor_WithLabelLongerThan128Characters_ThrowsArgumentException()
    {
        var label = new string('a', 129);

        Assert.Throws<ArgumentException>(() => new Detection(label, 0.5, ValidBoundingBox));
    }
}
