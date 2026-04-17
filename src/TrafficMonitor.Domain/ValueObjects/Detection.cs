namespace TrafficMonitor.Domain.ValueObjects;

public record Detection(string Label, double Confidence, BoundingBox BoundingBox)
{
    public string Label { get; init; } = ValidateLabel(Label);

    public double Confidence { get; init; } = ValidateConfidence(Confidence);

    public BoundingBox BoundingBox { get; init; } = ValidateBoundingBox(BoundingBox);

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
