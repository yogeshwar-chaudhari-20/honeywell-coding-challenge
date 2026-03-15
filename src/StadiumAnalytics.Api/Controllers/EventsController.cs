using Microsoft.AspNetCore.Mvc;
using StadiumAnalytics.Core.Dtos;
using StadiumAnalytics.Core.Events;
using StadiumAnalytics.Core.Models;

namespace StadiumAnalytics.Api.Controllers;

[ApiController]
[Route("api/v1/events")]
public sealed class EventsController : ControllerBase
{
    private readonly IGateEventChannel _channel;

    public EventsController(IGateEventChannel channel)
    {
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
        if (request is null)
        {
            ModelState.AddModelError(string.Empty, "Request body is required.");
            return ValidationProblem(ModelState);
        }

        StadiumGate? parsedGate = null;
        if (string.IsNullOrWhiteSpace(request.Gate))
        {
            ModelState.AddModelError(nameof(request.Gate), "Gate is required.");
        }
        else if (request.Gate.Length > 100)
        {
            ModelState.AddModelError(nameof(request.Gate), "Gate must not exceed 100 characters.");
        }
        else if (!StadiumGateExtensions.TryParseDisplayName(request.Gate, out var g))
        {
            var validGates = string.Join(", ", Enum.GetValues<StadiumGate>().Select(x => x.ToDisplayName()));
            ModelState.AddModelError(nameof(request.Gate), $"Invalid gate. Valid values: {validGates}");
        }
        else
        {
            parsedGate = g;
        }

        if (request.Timestamp is null)
        {
            ModelState.AddModelError(nameof(request.Timestamp), "Timestamp is required.");
        }

        if (request.NumberOfPeople is null)
        {
            ModelState.AddModelError(nameof(request.NumberOfPeople), "NumberOfPeople is required.");
        }
        else if (request.NumberOfPeople <= 0)
        {
            ModelState.AddModelError(nameof(request.NumberOfPeople), "NumberOfPeople must be greater than zero.");
        }

        GateEventType? parsedType = null;
        if (string.IsNullOrWhiteSpace(request.Type))
        {
            ModelState.AddModelError(nameof(request.Type), "Type is required.");
        }
        else if (!Enum.TryParse<GateEventType>(request.Type, ignoreCase: true, out var t))
        {
            ModelState.AddModelError(nameof(request.Type), "Invalid type. Valid values: enter, leave");
        }
        else
        {
            parsedType = t;
        }

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var sensorEvent = new GateSensorEvent(
            parsedGate!.Value,
            request.Timestamp!.Value,
            request.NumberOfPeople!.Value,
            parsedType!.Value);

        await _channel.PublishAsync(sensorEvent, cancellationToken);

        return Accepted();
    }
}
