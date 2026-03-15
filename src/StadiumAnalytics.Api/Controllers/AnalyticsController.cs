using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StadiumAnalytics.Core.Dtos;
using StadiumAnalytics.Core.Models;
using StadiumAnalytics.Infrastructure.Data;

namespace StadiumAnalytics.Api.Controllers;

[ApiController]
[Route("api/v1/analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly StadiumDbContext _dbContext;

    public AnalyticsController(StadiumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(List<AnalyticsSummaryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string? gate,
        [FromQuery] string? type,
        [FromQuery] DateTimeOffset? startTime,
        [FromQuery] DateTimeOffset? endTime,
        CancellationToken cancellationToken)
    {
        var filter = ParseAndValidate(gate, type, startTime, endTime);
        if (filter is null)
            return ValidationProblem(ModelState);

        var validationErrors = filter.Validate();
        if (validationErrors.Count > 0)
        {
            foreach (var error in validationErrors)
                ModelState.AddModelError(string.Empty, error);
            return ValidationProblem(ModelState);
        }

        var query = _dbContext.GateSensorEvents.AsQueryable();

        if (filter.Gate is not null)
            query = query.Where(e => e.Gate == filter.Gate);

        if (filter.Type is not null)
            query = query.Where(e => e.Type == filter.Type);

        if (filter.StartTime is not null)
        {
            var startStr = filter.StartTime.Value.ToUniversalTime().ToString("o");
            query = query.Where(e => string.Compare(e.Timestamp, startStr) >= 0);
        }

        if (filter.EndTime is not null)
        {
            var endStr = filter.EndTime.Value.ToUniversalTime().ToString("o");
            query = query.Where(e => string.Compare(e.Timestamp, endStr) <= 0);
        }

        var results = await query
            .GroupBy(e => new { e.Gate, e.Type })
            .Select(g => new
            {
                g.Key.Gate,
                g.Key.Type,
                NumberOfPeople = g.Sum(e => (long)e.NumberOfPeople)
            })
            .OrderBy(r => r.Gate)
            .ThenBy(r => r.Type)
            .ToListAsync(cancellationToken);

        var response = results.Select(r => new AnalyticsSummaryItem
        {
            Gate = r.Gate.ToDisplayName(),
            Type = r.Type.ToString().ToLowerInvariant(),
            NumberOfPeople = r.NumberOfPeople
        }).ToList();

        return Ok(response);
    }

    private SensorEventQuery? ParseAndValidate(
        string? gate, string? type, DateTimeOffset? startTime, DateTimeOffset? endTime)
    {
        StadiumGate? parsedGate = null;
        GateEventType? parsedType = null;

        if (gate is not null)
        {
            if (gate.Length > 100)
            {
                ModelState.AddModelError(nameof(gate), "Gate name must not exceed 100 characters.");
                return null;
            }

            if (!StadiumGateExtensions.TryParseDisplayName(gate, out var g))
            {
                var validGates = string.Join(", ", Enum.GetValues<StadiumGate>().Select(v => v.ToDisplayName()));
                ModelState.AddModelError(nameof(gate), $"Invalid gate. Valid values: {validGates}");
                return null;
            }

            parsedGate = g;
        }

        if (type is not null)
        {
            if (!Enum.TryParse<GateEventType>(type, ignoreCase: true, out var t))
            {
                ModelState.AddModelError(nameof(type), "Invalid type. Valid values: enter, leave");
                return null;
            }

            parsedType = t;
        }

        return new SensorEventQuery
        {
            Gate = parsedGate,
            Type = parsedType,
            StartTime = startTime,
            EndTime = endTime
        };
    }
}
