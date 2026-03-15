namespace StadiumAnalytics.Core.Dtos;

/// <summary>
/// Result of parsing and validating analytics summary query parameters.
/// </summary>
/// <param name="Query">Parsed query when validation succeeded; null when there are errors.</param>
/// <param name="Errors">Validation errors (Key = field name, Message = error text).</param>
public sealed record ParseSummaryQueryResult(
    SensorEventQuery? Query,
    IReadOnlyList<(string Key, string Message)> Errors);
