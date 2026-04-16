namespace TrafficMonitor.Domain.ValueObjects;

public sealed record BoundingBox
{
    private BoundingBox()
    {
    }

    public BoundingBox(decimal x, decimal y, decimal width, decimal height)
    {
        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "X must be non-negative.");
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(y), "Y must be non-negative.");
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public decimal X { get; private set; }

    public decimal Y { get; private set; }

    public decimal Width { get; private set; }

    public decimal Height { get; private set; }
}
