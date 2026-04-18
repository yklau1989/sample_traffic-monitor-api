using Microsoft.AspNetCore.Mvc;
using TrafficMonitor.Application.Commands.IngestTrafficEvent;

namespace TrafficMonitor.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController : ControllerBase
{
    private readonly IngestTrafficEventHandler _ingestHandler;

    public EventsController(IngestTrafficEventHandler ingestHandler)
    {
        _ingestHandler = ingestHandler;
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
}
