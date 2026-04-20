using System.ComponentModel.DataAnnotations;

namespace Backend.Api.Entities;

public sealed class TelemetrySpindleEntity
{
    [Key]
    public long Id { get; set; }

    public DateTimeOffset Ts { get; set; }

    [MaxLength(64)]
    public string MachineId { get; set; } = "";

    [MaxLength(64)]
    public string ToolId { get; set; } = "";

    public int SpindleRpm { get; set; }
    public float SpindleCurrentA { get; set; }
    public float SpindlePowerKw { get; set; }
    public int FeedMmMin { get; set; }

    [MaxLength(128)]
    public string? Program { get; set; }

    public bool CutFlag { get; set; }
}