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

    public float? ToolDiameterMm { get; init; }
    public float? SpindleTorqueNm { get; init; }

    // Новые поля состояния станка.
    // Collector может их передать явно.
    // Если не передаст, backend определит их сам.
    public string? MachineState { get; init; }
    public string? SpindleState { get; init; }
    public bool? StopRequired { get; init; }
    public string? StopReason { get; init; }
    public string? ControlAction { get; init; }
}