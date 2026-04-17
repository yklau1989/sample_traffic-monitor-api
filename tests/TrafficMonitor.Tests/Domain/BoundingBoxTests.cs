using TrafficMonitor.Domain.ValueObjects;

namespace TrafficMonitor.Tests.Domain;

public class BoundingBoxTests
{
    [Fact]
    public void Constructor_WithValidValues_Succeeds()
    {
        var boundingBox = new BoundingBox(1.5, 2.5, 3.5, 4.5);

        Assert.Equal(1.5, boundingBox.X);
        Assert.Equal(2.5, boundingBox.Y);
        Assert.Equal(3.5, boundingBox.Width);
        Assert.Equal(4.5, boundingBox.Height);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveWidth_ThrowsArgumentException(double width)
    {
        Assert.Throws<ArgumentException>(() => new BoundingBox(1, 2, width, 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveHeight_ThrowsArgumentException(double height)
    {
        Assert.Throws<ArgumentException>(() => new BoundingBox(1, 2, 3, height));
    }

    [Theory]
    [InlineData(double.NaN, 0, 1, 1)]
    [InlineData(0, double.NaN, 1, 1)]
    [InlineData(0, 0, double.NaN, 1)]
    [InlineData(0, 0, 1, double.NaN)]
    [InlineData(double.PositiveInfinity, 0, 1, 1)]
    [InlineData(0, double.NegativeInfinity, 1, 1)]
    [InlineData(0, 0, double.PositiveInfinity, 1)]
    [InlineData(0, 0, 1, double.NegativeInfinity)]
    public void Constructor_WithNonFiniteValues_ThrowsArgumentException(double x, double y, double width, double height)
    {
        Assert.Throws<ArgumentException>(() => new BoundingBox(x, y, width, height));
    }
}
