using StadiumAnalytics.Core.Models;

namespace StadiumAnalytics.Tests;

public class GateSensorEventTests
{
    [Fact]
    public void Constructor_WithValidInput_CreatesEvent()
    {
        var evt = new GateSensorEvent(StadiumGate.GateA, DateTimeOffset.UtcNow, 10, GateEventType.Enter);

        Assert.Equal(StadiumGate.GateA, evt.Gate);
        Assert.Equal(10, evt.NumberOfPeople);
        Assert.Equal(GateEventType.Enter, evt.Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidNumberOfPeople_Throws(int numberOfPeople)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GateSensorEvent(StadiumGate.GateA, DateTimeOffset.UtcNow, numberOfPeople, GateEventType.Enter));
    }

    [Fact]
    public void Constructor_WithInvalidEventType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GateSensorEvent(StadiumGate.GateA, DateTimeOffset.UtcNow, 10, (GateEventType)99));
    }

    [Fact]
    public void Constructor_WithInvalidGate_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GateSensorEvent((StadiumGate)99, DateTimeOffset.UtcNow, 10, GateEventType.Enter));
    }
}
