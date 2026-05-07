namespace Backend.Api.Services;

public sealed class FeatureExtractor
{
    public Dictionary<string, float> ExtractFromWindow(IReadOnlyList<CutRow> rows)
    {
        if (rows.Count == 0)
            throw new InvalidOperationException("Cannot extract features from empty window");

        var power = rows.Select(r => r.SpindlePowerKw).ToArray();
        var current = rows.Select(r => r.SpindleCurrentA).ToArray();
        var rpm = rows.Select(r => (float)r.SpindleRpm).ToArray();

        var v = rows.Select(r => r.CuttingSpeedMmin ?? 0f).ToArray();
        var ne = rows.Select(r => r.EffectivePowerKw ?? r.SpindlePowerKw).ToArray();
        var pz = rows.Select(r => r.TangentialForceN ?? 0f).ToArray();

        return new Dictionary<string, float>
        {
            ["p_mean"] = Mean(power),
            ["p_std"] = Std(power),
            ["p_slope"] = Slope(power),

            ["i_mean"] = Mean(current),
            ["i_std"] = Std(current),
            ["i_slope"] = Slope(current),

            ["rpm_mean"] = Mean(rpm),
            ["rpm_std"] = Std(rpm),
            ["rpm_slope"] = Slope(rpm),

            ["v_mean"] = Mean(v),
            ["v_std"] = Std(v),
            ["v_slope"] = Slope(v),

            ["ne_mean"] = Mean(ne),
            ["ne_std"] = Std(ne),
            ["ne_slope"] = Slope(ne),

            ["pz_mean"] = Mean(pz),
            ["pz_std"] = Std(pz),
            ["pz_slope"] = Slope(pz),
        };
    }

    private static float Mean(float[] x)
    {
        return x.Length == 0 ? 0f : (float)x.Average();
    }

    private static float Std(float[] x)
    {
        if (x.Length == 0)
            return 0f;

        var mean = Mean(x);
        var variance = x.Select(v => (v - mean) * (v - mean)).Average();

        return (float)Math.Sqrt(variance);
    }

    private static float Slope(float[] x)
    {
        var n = x.Length;

        if (n < 2)
            return 0f;

        double sumT = (n - 1) * n / 2.0;
        double sumT2 = (n - 1) * n * (2 * n - 1) / 6.0;
        double sumX = x.Sum(v => (double)v);

        double sumTX = 0.0;

        for (var i = 0; i < n; i++)
        {
            sumTX += i * x[i];
        }

        var denominator = n * sumT2 - sumT * sumT;

        if (Math.Abs(denominator) < 1e-9)
            return 0f;

        return (float)((n * sumTX - sumT * sumX) / denominator);
    }
}