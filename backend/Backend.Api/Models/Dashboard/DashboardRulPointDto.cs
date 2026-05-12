namespace Backend.Api.Models.Dashboard;

public sealed class DashboardRulPointDto
{
    public long Id { get; init; }
    public DateTimeOffset Ts { get; init; }

    public string MachineId { get; init; } = "";
    public string ToolId { get; init; } = "";

    public float RulMinutes { get; init; }
    public int AlarmLevel { get; init; }
    public string AlarmCode { get; init; } = "";
    public string State { get; init; } = "";
    public string Message { get; init; } = "";
    public string RequiredAction { get; init; } = "";
    public string ModelVersion { get; init; } = "";
}