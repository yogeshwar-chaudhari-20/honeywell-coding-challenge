using System.ComponentModel.DataAnnotations;

namespace StadiumAnalytics.Infrastructure.Simulation;

public sealed class EventSimulationOptions
{
    public const string SectionName = "EventSimulation";

    public bool Enabled { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int IntervalSeconds { get; set; } = 60;
}
