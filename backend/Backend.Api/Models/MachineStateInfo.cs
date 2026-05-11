namespace Backend.Api.Models;

public sealed class MachineStateInfo
{
    public string MachineState { get; init; } = "unknown";
    public string SpindleState { get; init; } = "unknown";
    public bool StopRequired { get; init; }
    public string? StopReason { get; init; }
    public string? ControlAction { get; init; }

    public string EventCode { get; init; } = "MACHINE_STATE_UNKNOWN";
    public int EventLevel { get; init; }
    public string EventMessage { get; init; } = "Состояние станка не определено.";
}