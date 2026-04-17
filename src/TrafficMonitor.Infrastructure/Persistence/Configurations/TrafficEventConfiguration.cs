using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrafficMonitor.Domain.Entities;

namespace TrafficMonitor.Infrastructure.Persistence.Configurations;

public class TrafficEventConfiguration : IEntityTypeConfiguration<TrafficEvent>
{
    public void Configure(EntityTypeBuilder<TrafficEvent> builder)
    {
        builder.HasKey(trafficEvent => trafficEvent.Id);

        builder.Property(trafficEvent => trafficEvent.Id)
            .ValueGeneratedOnAdd();

        builder.Property(trafficEvent => trafficEvent.EventId)
            .IsRequired();

        builder.HasIndex(trafficEvent => trafficEvent.EventId)
            .IsUnique();

        builder.Property(trafficEvent => trafficEvent.EventType)
            .HasConversion<string>();

        builder.Property(trafficEvent => trafficEvent.Severity)
            .HasConversion<string>();

        builder.Property(trafficEvent => trafficEvent.CameraId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(trafficEvent => trafficEvent.OccurredAt)
            .IsRequired();

        builder.Property(trafficEvent => trafficEvent.IngestedAt)
            .IsRequired();

        builder.Navigation(trafficEvent => trafficEvent.Detections)
            .HasField("_detections")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(trafficEvent => trafficEvent.Detections, owned =>
        {
            owned.OwnsOne(detection => detection.BoundingBox);
            owned.ToJson();
        });
    }
}
