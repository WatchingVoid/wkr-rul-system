import numpy as np
import pandas as pd

from feature_contract import FEATURE_ORDER


np.random.seed(42)


def main():
    rows = []

    life_count = 8
    points_per_life = 50

    for life_id in range(1, life_count + 1):
        tool_diameter_mm = np.random.choice([8.0, 10.0, 12.0])
        total_life_minutes = np.random.uniform(140.0, 220.0)

        base_rpm = np.random.uniform(6500.0, 8500.0)
        base_power = np.random.uniform(1.8, 2.8)
        base_current = np.random.uniform(8.0, 12.0)

        for idx in range(points_per_life):
            wear = idx / (points_per_life - 1)

            target_rul = max(
                0.0,
                total_life_minutes * (1.0 - wear) + np.random.normal(0, 4.0),
            )

            rpm_mean = base_rpm + np.random.normal(0, 35)
            rpm_std = 15 + 85 * wear + np.random.normal(0, 5)
            rpm_slope = np.random.normal(0, 1.5)

            p_mean = base_power + 5.5 * wear + np.random.normal(0, 0.15)
            p_std = 0.12 + 0.9 * wear + np.random.normal(0, 0.03)
            p_slope = 0.002 + 0.065 * wear + np.random.normal(0, 0.006)

            i_mean = base_current + 13.0 * wear + np.random.normal(0, 0.35)
            i_std = 0.18 + 1.15 * wear + np.random.normal(0, 0.05)
            i_slope = 0.001 + 0.045 * wear + np.random.normal(0, 0.005)

            # Только для демонстрационного датасета.
            # В рабочем контуре эти величины должен рассчитывать Backend/CuttingMath.cs.
            v_mean = np.pi * tool_diameter_mm * rpm_mean / 1000.0
            v_std = np.pi * tool_diameter_mm * rpm_std / 1000.0
            v_slope = np.pi * tool_diameter_mm * rpm_slope / 1000.0

            ne_mean = p_mean
            ne_std = p_std
            ne_slope = p_slope

            pz_mean = 60000.0 * ne_mean / max(v_mean, 1e-6)
            pz_std = 60000.0 * ne_std / max(v_mean, 1e-6)
            pz_slope = 60000.0 * ne_slope / max(v_mean, 1e-6)

            rows.append({
                "life_id": life_id,
                "tool_diameter_mm": tool_diameter_mm,

                "p_mean": p_mean,
                "p_std": p_std,
                "p_slope": p_slope,

                "i_mean": i_mean,
                "i_std": i_std,
                "i_slope": i_slope,

                "rpm_mean": rpm_mean,
                "rpm_std": rpm_std,
                "rpm_slope": rpm_slope,

                "v_mean": v_mean,
                "v_std": v_std,
                "v_slope": v_slope,

                "ne_mean": ne_mean,
                "ne_std": ne_std,
                "ne_slope": ne_slope,

                "pz_mean": pz_mean,
                "pz_std": pz_std,
                "pz_slope": pz_slope,

                "target_rul": target_rul,
            })

    df = pd.DataFrame(rows)

    cols = ["life_id", "tool_diameter_mm"] + FEATURE_ORDER + ["target_rul"]
    df = df[cols]

    df.to_csv("train_features.csv", index=False)

    print("Saved train_features.csv")
    print("Shape:", df.shape)
    print(df.head())


if __name__ == "__main__":
    main()