using System;

namespace Backend.Api.Services;

public sealed class FeatureExtractor
{
    public Dictionary<string, float> ExtractFromWindow(IReadOnlyList<CutRow> rows)
    {
        // rows: окно резания по времени (ASC)
        // Мы строим признаки по power/current/rpm + производным, если есть.
        float[] power = rows.Select(r => r.SpindlePowerKw).ToArray();
        float[] current = rows.Select(r => r.SpindleCurrentA).ToArray();
        float[] rpm = rows.Select(r => (float)r.SpindleRpm).ToArray();

        return new Dictionary<string, float>
        {
            ["p_mean"] = Mean(power),
            ["p_std"]  = Std(power),
            ["p_slope"] = Slope(power),

            ["i_mean"] = Mean(current),
            ["i_std"]  = Std(current),
            ["i_slope"] = Slope(current),

            //["rpm_mean"] = Mean(rpm),
            ["rpm_std"]  = Std(rpm),
            //["rpm_slope"] = Slope(rpm),

            // Можно добавить ещё:
            // ["p_max"] = power.Max(),
            // ["p_min"] = power.Min(),
        };
    }

    private static float Mean(float[] x) => x.Length == 0 ? 0 : (float)x.Average();
    private static float Std(float[] x)
    {
        if (x.Length == 0) return 0;
        var m = Mean(x);
        var v = x.Select(v => (v - m) * (v - m)).Average();
        return (float)Math.Sqrt(v);
    }

    // Линейный тренд (наклон) по времени
    private static float Slope(float[] x)
    {
        int n = x.Length;
        if (n < 2) return 0;

        // t = 0..n-1
        double sumT = (n - 1) * n / 2.0;
        double sumT2 = (n - 1) * n * (2 * n - 1) / 6.0;
        double sumX = x.Sum(v => (double)v);
        double sumTX = 0;
        for (int i = 0; i < n; i++) sumTX += i * x[i];

        // slope = (n*sumTX - sumT*sumX) / (n*sumT2 - sumT^2)
        double denom = n * sumT2 - sumT * sumT;
        if (Math.Abs(denom) < 1e-9) return 0;
        return (float)((n * sumTX - sumT * sumX) / denom);
    }
}