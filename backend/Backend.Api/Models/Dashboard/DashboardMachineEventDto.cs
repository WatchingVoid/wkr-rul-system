namespace Backend.Api.Models.Dashboard;

public sealed class DashboardMachineEventDto
{
    public long Id { get; init; }
    public DateTimeOffset Ts { get; init; }

    public string MachineId { get; init; } = "";
    public string? ToolId { get; init; }

    public string EventCode { get; init; } = "";
    public int EventLevel { get; init; }
    public string EventMessage { get; init; } = "";

    public string? MachineState { get; init; }
    public string? SpindleState { get; init; }
    public string? StopReason { get; init; }
    public string? ControlAction { get; init; }
}