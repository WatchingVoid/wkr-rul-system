using System;

namespace Backend.Api.Services;

public sealed record CuttingDerived(
    float? CuttingSpeedMMin,   // v, м/мин
    float? PowerFromTorqueKw,  // Ne, кВт (если есть M)
    float? TangentialForceN    // Pz, Н
);

public static class CuttingMath
{
    public static float? CuttingSpeedMMin(float? toolDiameterMm, int spindleRpm)
    {
        if (toolDiameterMm is null || toolDiameterMm <= 0) return null;
        if (spindleRpm <= 0) return null;
        return (float)(Math.PI * toolDiameterMm.Value * spindleRpm / 1000.0);
    }

    public static float? PowerFromTorqueKw(float? torqueNm, int spindleRpm)
    {
        if (torqueNm is null || torqueNm < 0) return null;
        if (spindleRpm <= 0) return null;
        return (float)(torqueNm.Value * 2.0 * Math.PI * spindleRpm / 60.0 / 1000.0);
    }

    public static float? TangentialForceN(float? effectivePowerKw, float? cuttingSpeedMMin)
    {
        if (effectivePowerKw is null || effectivePowerKw < 0) return null;
        if (cuttingSpeedMMin is null || cuttingSpeedMMin <= 0) return null;

        var pcW = effectivePowerKw.Value * 1000.0f;
        return pcW * 60.0f / cuttingSpeedMMin.Value;
    }

    public static CuttingDerived Compute(int spindleRpm, float? toolDiameterMm, float? torqueNm, float? spindlePowerKw)
    {
        var v = CuttingSpeedMMin(toolDiameterMm, spindleRpm);
        var neFromTorque = PowerFromTorqueKw(torqueNm, spindleRpm);
        var neEffective = neFromTorque ?? spindlePowerKw;
        var pz = TangentialForceN(neEffective, v);

        return new CuttingDerived(v, neFromTorque, pz);
    }
}