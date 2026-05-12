namespace Backend.Api.Models.Dashboard;

public sealed class DashboardTelemetryPointDto
{
    public long Id { get; init; }
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

    // Формульные параметры из ВКР
    public float? CuttingSpeedMmin { get; init; }       // v
    public float? PowerFromTorqueKw { get; init; }      // Ne
    public float? TangentialForceN { get; init; }       // Pz

    // Состояние станка
    public string MachineState { get; init; } = "unknown";
    public string SpindleState { get; init; } = "unknown";
    public bool StopRequired { get; init; }
    public string? StopReason { get; init; }
    public string? ControlAction { get; init; }

    // Удобные признаки для клиента
    public bool IsCutting { get; init; }
    public bool IsStopped { get; init; }
}