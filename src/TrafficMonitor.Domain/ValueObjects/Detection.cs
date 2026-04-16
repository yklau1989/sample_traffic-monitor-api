namespace TrafficMonitor.Domain.ValueObjects;

public sealed record Detection
{
    private Detection()
    {
    }

    public Detection(string label, decimal confidence, BoundingBox boundingBox)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Label is required.", nameof(label));
        }

        if (confidence < 0 || confidence > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");
        }

        Label = label.Trim();
        Confidence = confidence;
        BoundingBox = boundingBox ?? throw new ArgumentNullException(nameof(boundingBox));
    }

    public string Label { get; private set; } = string.Empty;

    public decimal Confidence { get; private set; }

    public BoundingBox BoundingBox { get; private set; } = new(0, 0, 1, 1);
}
