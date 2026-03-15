namespace StadiumAnalytics.Core.Models;

public enum StadiumGate
{
    GateA,
    GateB,
    GateC,
    GateD,
    GateE
}

public static class StadiumGateExtensions
{
    private static readonly IReadOnlyDictionary<string, StadiumGate> DisplayNameToGate =
        new Dictionary<string, StadiumGate>(StringComparer.OrdinalIgnoreCase)
        {
            ["Gate A"] = StadiumGate.GateA,
            ["Gate B"] = StadiumGate.GateB,
            ["Gate C"] = StadiumGate.GateC,
            ["Gate D"] = StadiumGate.GateD,
            ["Gate E"] = StadiumGate.GateE,
        };

    private static readonly IReadOnlyDictionary<StadiumGate, string> GateToDisplayName =
        new Dictionary<StadiumGate, string>
        {
            [StadiumGate.GateA] = "Gate A",
            [StadiumGate.GateB] = "Gate B",
            [StadiumGate.GateC] = "Gate C",
            [StadiumGate.GateD] = "Gate D",
            [StadiumGate.GateE] = "Gate E",
        };

    public static string ToDisplayName(this StadiumGate gate)
    {
        return GateToDisplayName[gate];
    }

    public static bool TryParseDisplayName(string? displayName, out StadiumGate gate)
    {
        if (displayName is not null && DisplayNameToGate.TryGetValue(displayName, out gate))
            return true;

        gate = default;
        return false;
    }
}
