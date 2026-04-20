namespace Backend.Api.Models;

public sealed class TelemetryFrame
{
    public DateTimeOffset Ts { get; init; }
    public string MachineId { get; init; } = "";
    public string ToolId { get; init; } = "";
    public int SpindleRpm { get; init; }
    public float SpindleCurrentA { get; init; }
    public float SpindlePowerKw { get; init; }
    public int FeedMmMin { get; init; }
    public string? Program { get; init; }
    public bool CutFlag { get; init; }
}