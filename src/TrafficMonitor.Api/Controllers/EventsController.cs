using Microsoft.AspNetCore.Mvc;
using TrafficMonitor.Application.Commands.IngestTrafficEvent;
using TrafficMonitor.Application.Queries.ListTrafficEvents;
using TrafficMonitor.Domain.Enums;

namespace TrafficMonitor.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController : ControllerBase
{
    private readonly IngestTrafficEventHandler _ingestHandler;
    private readonly ListTrafficEventsHandler _listHandler;

    public EventsController(
        IngestTrafficEventHandler ingestHandler,
        ListTrafficEventsHandler listHandler)
    {
        _ingestHandler = ingestHandler;
        _listHandler = listHandler;
    }

    [HttpPost]
    public async Task<IActionResult> IngestAsync(
        [FromBody] TrafficEventInput input,
        CancellationToken cancellationToken)
    {
        var command = new IngestTrafficEventCommand(input);
        var result = await _ingestHandler.HandleAsync(command, cancellationToken);

        var location = $"/api/events/{result.EventId}";
        var body = new { eventId = result.EventId };

        if (result.WasDuplicate)
        {
            Response.Headers.Location = location;
            return StatusCode(200, body);
        }

        return Created(location, body);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<EventListItemDto>>> ListAsync(
        [FromQuery] ListEventsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.From is { Kind: not DateTimeKind.Utc })
        {
            return Problem(
                statusCode: 400,
                title: "Invalid timestamp",
                detail: "The from query parameter must be a UTC timestamp.");
        }

        if (request.To is { Kind: not DateTimeKind.Utc })
        {
            return Problem(
                statusCode: 400,
                title: "Invalid timestamp",
                detail: "The to query parameter must be a UTC timestamp.");
        }

        var page = Math.Max(request.Page ?? 1, 1);
        var pageSize = Math.Clamp(request.PageSize ?? 50, 1, 200);
        var query = new ListTrafficEventsQuery(
            request.EventType,
            request.Severity,
            request.From,
            request.To,
            request.CameraId,
            request.Sort ?? "occurredAt",
            page,
            pageSize);

        var result = await _listHandler.HandleAsync(query, cancellationToken);

        return Ok(result);
    }
}

public sealed record ListEventsRequest(
    EventType? EventType = null,
    Severity? Severity = null,
    DateTime? From = null,
    DateTime? To = null,
    string? CameraId = null,
    string? Sort = null,
    int? Page = 1,
    int? PageSize = 50);
