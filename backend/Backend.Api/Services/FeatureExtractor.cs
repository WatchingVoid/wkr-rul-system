using Backend.Api.Models;

namespace Backend.Api.Services;

public sealed class FeatureExtractor
{
    public Dictionary<string, float> ExtractFromWindow(IReadOnlyList<CutWindowRow> rows)
    {
        if (rows.Count == 0)
            throw new ArgumentException("Cut window is empty", nameof(rows));

        var p = rows.Select(x => (double)x.SpindlePowerKw).ToArray();
        var i = rows.Select(x => (double)x.SpindleCurrentA).ToArray();
        var rpm = rows.Select(x => (double)x.SpindleRpm).ToArray();

        var v = rows.Select(x => (double)(x.CuttingSpeedMmin ?? 0f)).ToArray();
        var ne = rows.Select(x => (double)x.SpindlePowerKw).ToArray();
        var pz = rows.Select(x => (double)(x.TangentialForceN ?? 0f)).ToArray();

        return new Dictionary<string, float>
        {
            ["p_mean"] = Mean(p),
            ["p_std"] = Std(p),
            ["p_slope"] = Slope(p),

            ["i_mean"] = Mean(i),
            ["i_std"] = Std(i),
            ["i_slope"] = Slope(i),

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

    private static float Mean(double[] values)
    {
        if (values.Length == 0)
            return 0f;

        return (float)values.Average();
    }

    private static float Std(double[] values)
    {
        if (values.Length == 0)
            return 0f;

        var mean = values.Average();
        var variance = values.Select(x => Math.Pow(x - mean, 2)).Average();

        return (float)Math.Sqrt(variance);
    }

    private static float Slope(double[] values)
    {
        if (values.Length < 2)
            return 0f;

        var n = values.Length;
        var xMean = (n - 1) / 2.0;
        var yMean = values.Average();

        double numerator = 0;
        double denominator = 0;

        for (var index = 0; index < n; index++)
        {
            var dx = index - xMean;
            var dy = values[index] - yMean;

            numerator += dx * dy;
            denominator += dx * dx;
        }

        if (Math.Abs(denominator) < 0.000001)
            return 0f;

        return (float)(numerator / denominator);
    }
}