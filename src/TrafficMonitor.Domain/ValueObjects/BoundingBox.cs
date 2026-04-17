namespace TrafficMonitor.Domain.ValueObjects;

public record BoundingBox(double X, double Y, double Width, double Height)
{
    public double X { get; init; } = ValidateFinite(X, nameof(X));

    public double Y { get; init; } = ValidateFinite(Y, nameof(Y));

    public double Width { get; init; } = ValidateDimension(Width, nameof(Width));

    public double Height { get; init; } = ValidateDimension(Height, nameof(Height));

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
