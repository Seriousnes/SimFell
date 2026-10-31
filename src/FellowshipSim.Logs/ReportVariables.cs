using System.Text.Json.Serialization;

namespace FellowshipSim.Logs;

/// <summary>Variables of the report summary query.</summary>
public sealed record ReportVariables(
    [property: JsonPropertyName("code")] string Code);
