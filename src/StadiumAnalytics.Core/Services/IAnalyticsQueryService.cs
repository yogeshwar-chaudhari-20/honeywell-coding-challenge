using StadiumAnalytics.Core.Dtos;
using StadiumAnalytics.Core.Models;

namespace StadiumAnalytics.Core.Services;

/// <summary>
/// Parses summary query parameters and executes analytics queries.
/// </summary>
public interface IAnalyticsQueryService
{
    /// <summary>
    /// Parses and validates raw query parameters into a <see cref="SensorEventQuery"/>.
    /// </summary>
    ParseSummaryQueryResult TryParseSummaryQuery(
        string? gate,
        string? type,
        DateTimeOffset? startTime,
        DateTimeOffset? endTime);

    /// <summary>
    /// Returns aggregated summary by gate and type for the given query.
    /// </summary>
    Task<IReadOnlyList<AnalyticsSummaryItem>> GetSummaryAsync(
        SensorEventQuery query,
        CancellationToken cancellationToken = default);
}
