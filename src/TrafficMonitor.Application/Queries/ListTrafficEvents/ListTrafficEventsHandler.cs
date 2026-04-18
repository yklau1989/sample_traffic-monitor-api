using TrafficMonitor.Application.Repositories;

namespace TrafficMonitor.Application.Queries.ListTrafficEvents;

public sealed class ListTrafficEventsHandler
{
    private readonly ITrafficEventRepository _repository;

    public ListTrafficEventsHandler(ITrafficEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<EventListItemDto>> HandleAsync(
        ListTrafficEventsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await _repository.ListAsync(query, cancellationToken);
    }
}
