namespace Backend.Api.Models.Dashboard;

public sealed class DashboardCurrentDto
{
    public string MachineId { get; init; } = "";
    public string ToolId { get; init; } = "";

    public DashboardTelemetryPointDto? LastTelemetry { get; init; }
    public DashboardRulPointDto? LastPrediction { get; init; }
    public DashboardAlarmDto? LastAlarm { get; init; }

    public IReadOnlyList<DashboardMachineEventDto> MachineEvents { get; init; } = [];
}