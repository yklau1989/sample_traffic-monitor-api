namespace TrafficMonitor.Application.Queries.ListTrafficEvents;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize);
