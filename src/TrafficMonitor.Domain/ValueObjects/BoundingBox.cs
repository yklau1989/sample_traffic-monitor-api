namespace TrafficMonitor.Domain.ValueObjects;

public class BoundingBox : IEquatable<BoundingBox>
{
    public double X { get; private init; }

    public double Y { get; private init; }

    public double Width { get; private init; }

    public double Height { get; private init; }

    private BoundingBox()
    {
    }

    public BoundingBox(double x, double y, double width, double height)
    {
        X = ValidateFinite(x, nameof(X));
        Y = ValidateFinite(y, nameof(Y));
        Width = ValidateDimension(width, nameof(Width));
        Height = ValidateDimension(height, nameof(Height));
    }

    public bool Equals(BoundingBox? other) =>
        other is not null
        && X.Equals(other.X)
        && Y.Equals(other.Y)
        && Width.Equals(other.Width)
        && Height.Equals(other.Height);

    public override bool Equals(object? obj) => Equals(obj as BoundingBox);

    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static double ValidateFinite(double value, string paramName)
    {
        if (!IsFinite(value))
        {
            throw new ArgumentException($"{paramName} must be finite.", paramName);
        }

        return value;
    }

    private static double ValidateDimension(double value, string paramName)
    {
        ValidateFinite(value, paramName);

        if (value <= 0)
        {
            throw new ArgumentException($"{paramName} must be greater than zero.", paramName);
        }

        return value;
    }
}
