using Microsoft.AspNetCore.Mvc;
using StadiumAnalytics.Core.Dtos;
using StadiumAnalytics.Core.Events;
using StadiumAnalytics.Core.Services;

namespace StadiumAnalytics.Api.Controllers;

[ApiController]
[Route("api/v1/events")]
public sealed class EventsController : ControllerBase
{
    private readonly IEventIngestionService _eventIngestionService;
    private readonly IGateEventChannel _channel;

    public EventsController(IEventIngestionService eventIngestionService, IGateEventChannel channel)
    {
        _eventIngestionService = eventIngestionService;
        _channel = channel;
    }

    /// <summary>
    /// Returns 405; use POST to ingest events.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status405MethodNotAllowed)]
    public IActionResult Get()
    {
        Response.Headers.Allow = "POST";
        return new ObjectResult("Use POST with a JSON body to ingest a sensor event.")
            { StatusCode = StatusCodes.Status405MethodNotAllowed };
    }

    /// <summary>
    /// Ingest a single gate sensor event from an external source. The event is accepted asynchronously
    /// and processed by the event consumer (persisted or recorded as duplicate/failed).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ingest(
        [FromBody] SensorEventIngressRequest? request,
        CancellationToken cancellationToken)
    {
        var result = _eventIngestionService.ValidateAndMap(request!);

        if (!result.IsValid)
        {
            foreach (var (key, message) in result.Errors)
                ModelState.AddModelError(key, message);
            return ValidationProblem(ModelState);
        }

        await _channel.PublishAsync(result.Event!, cancellationToken);
        return Accepted();
    }
}
