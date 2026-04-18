using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TrafficMonitor.Domain.Entities;
using TrafficMonitor.Domain.ValueObjects;

namespace TrafficMonitor.Infrastructure.Persistence.Configurations;

public class TrafficEventConfiguration : IEntityTypeConfiguration<TrafficEvent>
{
    public void Configure(EntityTypeBuilder<TrafficEvent> builder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var detectionsConverter = new ValueConverter<List<Detection>, string>(
            detections => JsonSerializer.Serialize(detections, jsonOptions),
            json => JsonSerializer.Deserialize<List<Detection>>(json, jsonOptions) ?? new List<Detection>());

        var detectionsComparer = new ValueComparer<List<Detection>>(
            (a, b) => JsonSerializer.Serialize(a, jsonOptions) == JsonSerializer.Serialize(b, jsonOptions),
            detections => JsonSerializer.Serialize(detections, jsonOptions).GetHashCode(),
            detections => JsonSerializer.Deserialize<List<Detection>>(
                JsonSerializer.Serialize(detections, jsonOptions), jsonOptions) ?? new List<Detection>());

        builder.HasKey(trafficEvent => trafficEvent.Id);
        builder.Ignore(trafficEvent => trafficEvent.Detections);

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

        builder.Property<List<Detection>>("_detections")
            .HasField("_detections")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(detectionsConverter, detectionsComparer)
            .HasColumnName("detections")
            .HasColumnType("jsonb");
    }
}
