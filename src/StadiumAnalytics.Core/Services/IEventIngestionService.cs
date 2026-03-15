using StadiumAnalytics.Core.Dtos;

namespace StadiumAnalytics.Core.Services;

/// <summary>
/// Validates and maps event ingestion requests to domain events.
/// </summary>
public interface IEventIngestionService
{
    /// <summary>
    /// Validates the request and, if valid, produces a <see cref="Models.GateSensorEvent"/>.
    /// </summary>
    EventIngestionResult ValidateAndMap(SensorEventIngressRequest request);
}
