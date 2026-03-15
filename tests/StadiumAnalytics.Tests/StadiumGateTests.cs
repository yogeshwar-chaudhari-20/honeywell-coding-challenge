using StadiumAnalytics.Core.Models;

namespace StadiumAnalytics.Tests;

public class StadiumGateTests
{
    [Theory]
    [InlineData("Gate A", StadiumGate.GateA)]
    [InlineData("Gate B", StadiumGate.GateB)]
    [InlineData("Gate C", StadiumGate.GateC)]
    [InlineData("Gate D", StadiumGate.GateD)]
    [InlineData("Gate E", StadiumGate.GateE)]
    public void TryParseDisplayName_WithValidInput_ReturnsTrue(string input, StadiumGate expected)
    {
        Assert.True(StadiumGateExtensions.TryParseDisplayName(input, out var gate));
        Assert.Equal(expected, gate);
    }

    [Theory]
    [InlineData("gate a")]
    [InlineData("GATE A")]
    public void TryParseDisplayName_IsCaseInsensitive(string input)
    {
        Assert.True(StadiumGateExtensions.TryParseDisplayName(input, out _));
    }

    [Theory]
    [InlineData("Gate Z")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("GateA")]
    public void TryParseDisplayName_WithInvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(StadiumGateExtensions.TryParseDisplayName(input, out _));
    }

    [Theory]
    [InlineData(StadiumGate.GateA, "Gate A")]
    [InlineData(StadiumGate.GateE, "Gate E")]
    public void ToDisplayName_ReturnsCorrectString(StadiumGate gate, string expected)
    {
        Assert.Equal(expected, gate.ToDisplayName());
    }
}
