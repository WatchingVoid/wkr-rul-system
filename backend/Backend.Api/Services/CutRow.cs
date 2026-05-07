namespace Backend.Api.Services;

public sealed class CutRow
{
    public DateTimeOffset Ts { get; set; }

    public int SpindleRpm { get; set; }
    public float SpindleCurrentA { get; set; }
    public float SpindlePowerKw { get; set; }

    public float? CuttingSpeedMmin { get; set; }
    public float? EffectivePowerKw { get; set; }
    public float? TangentialForceN { get; set; }
}