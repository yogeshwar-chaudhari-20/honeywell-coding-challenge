using StadiumAnalytics.Core.Models;

namespace StadiumAnalytics.Core.Dtos;

/// <summary>
/// Result of validating and mapping an event ingestion request to a domain event.
/// </summary>
public sealed record EventIngestionResult(
    bool IsValid,
    GateSensorEvent? Event,
    IReadOnlyList<(string Key, string Message)> Errors);
