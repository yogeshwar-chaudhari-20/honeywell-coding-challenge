using StadiumAnalytics.Core.Dtos;
using StadiumAnalytics.Core.Models;
using StadiumAnalytics.Infrastructure.Services;

namespace StadiumAnalytics.Tests;

public class EventIngestionServiceTests
{
    private readonly EventIngestionService _service = new();

    [Fact]
    public void ValidateAndMap_NullRequest_ReturnsInvalid()
    {
        var result = _service.ValidateAndMap(null!);

        Assert.False(result.IsValid);
        Assert.Null(result.Event);
        Assert.Single(result.Errors);
        Assert.Contains("Request body is required", result.Errors[0].Message);
    }

    [Fact]
    public void ValidateAndMap_ValidRequest_ReturnsEvent()
    {
        var request = new SensorEventIngressRequest
        {
            Gate = "Gate A",
            Timestamp = new DateTimeOffset(2023, 4, 1, 8, 0, 0, TimeSpan.Zero),
            NumberOfPeople = 10,
            Type = "enter"
        };

        var result = _service.ValidateAndMap(request);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Event);
        Assert.Empty(result.Errors);
        Assert.Equal(StadiumGate.GateA, result.Event!.Gate);
        Assert.Equal(GateEventType.Enter, result.Event.Type);
        Assert.Equal(10, result.Event.NumberOfPeople);
    }

    [Fact]
    public void ValidateAndMap_MissingGate_ReturnsErrors()
    {
        var request = new SensorEventIngressRequest
        {
            Gate = "",
            Timestamp = DateTimeOffset.UtcNow,
            NumberOfPeople = 5,
            Type = "leave"
        };

        var result = _service.ValidateAndMap(request);

        Assert.False(result.IsValid);
        Assert.Null(result.Event);
        Assert.Contains(result.Errors, e => e.Key.Contains("Gate") && e.Message.Contains("required"));
    }

    [Fact]
    public void ValidateAndMap_InvalidGate_ReturnsErrors()
    {
        var request = new SensorEventIngressRequest
        {
            Gate = "Gate Z",
            Timestamp = DateTimeOffset.UtcNow,
            NumberOfPeople = 1,
            Type = "enter"
        };

        var result = _service.ValidateAndMap(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("Valid values"));
    }

    [Fact]
    public void ValidateAndMap_InvalidType_ReturnsErrors()
    {
        var request = new SensorEventIngressRequest
        {
            Gate = "Gate A",
            Timestamp = DateTimeOffset.UtcNow,
            NumberOfPeople = 1,
            Type = "jump"
        };

        var result = _service.ValidateAndMap(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Key.Contains("Type"));
    }

    [Fact]
    public void ValidateAndMap_NumberOfPeopleZero_ReturnsErrors()
    {
        var request = new SensorEventIngressRequest
        {
            Gate = "Gate A",
            Timestamp = DateTimeOffset.UtcNow,
            NumberOfPeople = 0,
            Type = "enter"
        };

        var result = _service.ValidateAndMap(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Message.Contains("greater than zero"));
    }

    [Fact]
    public void ValidateAndMap_MissingTimestamp_ReturnsErrors()
    {
        var request = new SensorEventIngressRequest
        {
            Gate = "Gate A",
            Timestamp = null,
            NumberOfPeople = 1,
            Type = "enter"
        };

        var result = _service.ValidateAndMap(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Key.Contains("Timestamp"));
    }

    [Fact]
    public void ValidateAndMap_TypeCaseInsensitive_AcceptsEnter()
    {
        var request = new SensorEventIngressRequest
        {
            Gate = "Gate B",
            Timestamp = DateTimeOffset.UtcNow,
            NumberOfPeople = 1,
            Type = "ENTER"
        };

        var result = _service.ValidateAndMap(request);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Event);
        Assert.Equal(GateEventType.Enter, result.Event!.Type);
    }
}
