namespace TrafficMonitor.Domain.ValueObjects;

public class Detection : IEquatable<Detection>
{
    public string Label { get; private init; } = string.Empty;

    public double Confidence { get; private init; }

    public BoundingBox BoundingBox { get; private init; } = default!;

    private Detection()
    {
        Label = string.Empty;
        BoundingBox = default!;
    }

    public Detection(string label, double confidence, BoundingBox boundingBox)
    {
        Label = ValidateLabel(label);
        Confidence = ValidateConfidence(confidence);
        BoundingBox = ValidateBoundingBox(boundingBox);
    }

    public bool Equals(Detection? other) =>
        other is not null
        && Label == other.Label
        && Confidence.Equals(other.Confidence)
        && EqualityComparer<BoundingBox>.Default.Equals(BoundingBox, other.BoundingBox);

    public override bool Equals(object? obj) => Equals(obj as Detection);

    public override int GetHashCode() => HashCode.Combine(Label, Confidence, BoundingBox);

    private static string ValidateLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Label is required.", nameof(Label));
        }

        var trimmedLabel = label.Trim();

        if (trimmedLabel.Length > 128)
        {
            throw new ArgumentException("Label cannot exceed 128 characters.", nameof(Label));
        }

        return trimmedLabel;
    }

    private static double ValidateConfidence(double confidence)
    {
        if (confidence < 0.0 || confidence > 1.0)
        {
            throw new ArgumentException("Confidence must be between 0.0 and 1.0.", nameof(Confidence));
        }

        return confidence;
    }

    private static BoundingBox ValidateBoundingBox(BoundingBox boundingBox)
    {
        if (boundingBox is null)
        {
            throw new ArgumentException("BoundingBox is required.", nameof(BoundingBox));
        }

        return boundingBox;
    }
}
