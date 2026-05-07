namespace Backend.Api.Services;

public sealed record CuttingDerived(
    float? CuttingSpeedMmin,
    float? PowerFromTorqueKw,
    float? TangentialForceN
);

public static class CuttingMath
{
    public static CuttingDerived Compute(
        int spindleRpm,
        float? toolDiameterMm,
        float? torqueNm,
        float spindlePowerKw)
    {
        var cuttingSpeed = ComputeCuttingSpeedMmin(toolDiameterMm, spindleRpm);

        var powerFromTorque = ComputePowerFromTorqueKw(torqueNm, spindleRpm);

        var effectivePower = powerFromTorque ?? spindlePowerKw;

        var tangentialForce = ComputeTangentialForceN(effectivePower, cuttingSpeed);

        return new CuttingDerived(
            CuttingSpeedMmin: cuttingSpeed,
            PowerFromTorqueKw: powerFromTorque,
            TangentialForceN: tangentialForce
        );
    }

    public static float? ComputeCuttingSpeedMmin(float? toolDiameterMm, int spindleRpm)
    {
        if (toolDiameterMm is null || toolDiameterMm <= 0 || spindleRpm <= 0)
            return null;

        // v = π · D · n / 1000
        return (float)(Math.PI * toolDiameterMm.Value * spindleRpm / 1000.0);
    }

    public static float? ComputePowerFromTorqueKw(float? torqueNm, int spindleRpm)
    {
        if (torqueNm is null || torqueNm <= 0 || spindleRpm <= 0)
            return null;

        // Ne = M · n / 9550
        return torqueNm.Value * spindleRpm / 9550.0f;
    }

    public static float? ComputeTangentialForceN(float? effectivePowerKw, float? cuttingSpeedMmin)
    {
        if (effectivePowerKw is null || effectivePowerKw <= 0)
            return null;

        if (cuttingSpeedMmin is null || cuttingSpeedMmin <= 0)
            return null;

        // Pz = 60000 · Ne / v
        return 60000.0f * effectivePowerKw.Value / cuttingSpeedMmin.Value;
    }
}