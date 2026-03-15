using Microsoft.AspNetCore.Mvc;
using StadiumAnalytics.Core.Dtos;
using StadiumAnalytics.Core.Services;

namespace StadiumAnalytics.Api.Controllers;

[ApiController]
[Route("api/v1/analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsQueryService _analyticsQueryService;

    public AnalyticsController(IAnalyticsQueryService analyticsQueryService)
    {
        _analyticsQueryService = analyticsQueryService;
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
        var parseResult = _analyticsQueryService.TryParseSummaryQuery(gate, type, startTime, endTime);

        if (parseResult.Errors.Count > 0)
        {
            foreach (var (key, message) in parseResult.Errors)
                ModelState.AddModelError(key, message);
            return ValidationProblem(ModelState);
        }

        var results = await _analyticsQueryService.GetSummaryAsync(parseResult.Query!, cancellationToken);
        return Ok(results);
    }
}
