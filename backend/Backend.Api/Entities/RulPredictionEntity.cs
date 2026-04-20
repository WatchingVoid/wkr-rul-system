using System.ComponentModel.DataAnnotations;

namespace Backend.Api.Entities;

public sealed class RulPredictionEntity
{
    [Key]
    public long Id { get; set; }

    public DateTimeOffset Ts { get; set; }

    [MaxLength(64)]
    public string MachineId { get; set; } = "";

    [MaxLength(64)]
    public string ToolId { get; set; } = "";

    public float RulMinutes { get; set; }
    public int AlarmLevel { get; set; } // 0 ok, 1 warn, 2 stop

    [MaxLength(32)]
    public string ModelVersion { get; set; } = "dev";
}