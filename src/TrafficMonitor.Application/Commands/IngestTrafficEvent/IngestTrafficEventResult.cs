namespace TrafficMonitor.Application.Commands.IngestTrafficEvent;

public sealed record IngestTrafficEventResult(Guid EventId, bool WasDuplicate);
