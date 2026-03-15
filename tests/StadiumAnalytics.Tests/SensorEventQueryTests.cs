using StadiumAnalytics.Core.Dtos;
using StadiumAnalytics.Core.Models;

namespace StadiumAnalytics.Tests;

public class SensorEventQueryTests
{
    [Fact]
    public void Validate_NoFilters_ReturnsNoErrors()
    {
        var query = new SensorEventQuery();
        Assert.Empty(query.Validate());
    }

    [Fact]
    public void Validate_ValidTimeRange_ReturnsNoErrors()
    {
        var query = new SensorEventQuery
        {
            StartTime = DateTimeOffset.UtcNow.AddHours(-1),
            EndTime = DateTimeOffset.UtcNow
        };

        Assert.Empty(query.Validate());
    }

    [Fact]
    public void Validate_StartTimeAfterEndTime_ReturnsError()
    {
        var query = new SensorEventQuery
        {
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(-1)
        };

        var errors = query.Validate();
        Assert.Single(errors);
        Assert.Contains("startTime", errors[0]);
    }

    [Fact]
    public void Validate_WithGateAndType_ReturnsNoErrors()
    {
        var query = new SensorEventQuery
        {
            Gate = StadiumGate.GateA,
            Type = GateEventType.Enter
        };

        Assert.Empty(query.Validate());
    }
}
