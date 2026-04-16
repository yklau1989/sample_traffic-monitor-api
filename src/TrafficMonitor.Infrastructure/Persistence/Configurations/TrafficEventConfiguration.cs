namespace TrafficMonitor.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrafficMonitor.Domain.Entities;
using TrafficMonitor.Domain.ValueObjects;

public sealed class TrafficEventConfiguration : IEntityTypeConfiguration<TrafficEvent>
{
    public void Configure(EntityTypeBuilder<TrafficEvent> builder)
    {
        builder.ToTable("traffic_events");

        builder.HasKey(trafficEvent => trafficEvent.Id);

        builder.Property(trafficEvent => trafficEvent.Id)
            .HasColumnName("id");

        builder.Property(trafficEvent => trafficEvent.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.HasIndex(trafficEvent => trafficEvent.EventId)
            .IsUnique();

        builder.Property(trafficEvent => trafficEvent.CameraId)
            .HasColumnName("camera_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(trafficEvent => trafficEvent.EventType)
            .HasColumnName("event_type")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(trafficEvent => trafficEvent.Severity)
            .HasColumnName("severity")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(trafficEvent => trafficEvent.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Navigation(trafficEvent => trafficEvent.Detections)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(trafficEvent => trafficEvent.Detections, detectionBuilder =>
        {
            detectionBuilder.ToJson("detections");
            detectionBuilder.Property(detection => detection.Label)
                .HasMaxLength(64)
                .IsRequired();
            detectionBuilder.Property(detection => detection.Confidence)
                .HasPrecision(5, 4)
                .IsRequired();
            detectionBuilder.OwnsOne(detection => detection.BoundingBox, boundingBoxBuilder =>
            {
                boundingBoxBuilder.Property(boundingBox => boundingBox.X)
                    .HasPrecision(7, 2);
                boundingBoxBuilder.Property(boundingBox => boundingBox.Y)
                    .HasPrecision(7, 2);
                boundingBoxBuilder.Property(boundingBox => boundingBox.Width)
                    .HasPrecision(7, 2);
                boundingBoxBuilder.Property(boundingBox => boundingBox.Height)
                    .HasPrecision(7, 2);
            });
        });
    }
}
