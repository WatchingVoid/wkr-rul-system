import numpy as np
import pandas as pd

np.random.seed(42)

rows = []

# Генерируем демонстрационные окна разного состояния инструмента
# Чем выше мощность/ток/тренд, тем меньше остаточный ресурс.
for i in range(120):
    wear = i / 119.0  # 0..1

    p_mean = 2.0 + 5.0 * wear + np.random.normal(0, 0.15)
    p_std = 0.15 + 0.8 * wear + np.random.normal(0, 0.03)
    p_slope = 0.001 + 0.06 * wear + np.random.normal(0, 0.005)

    i_mean = 10.0 + 12.0 * wear + np.random.normal(0, 0.4)
    i_std = 0.2 + 1.0 * wear + np.random.normal(0, 0.05)
    i_slope = 0.001 + 0.04 * wear + np.random.normal(0, 0.004)

    rpm_mean = 8200 + np.random.normal(0, 40)
    rpm_std = 20 + 80 * wear + np.random.normal(0, 5)
    rpm_slope = np.random.normal(0, 1.5)

    # Условная целевая RUL: чем больше wear, тем меньше ресурс
    target_rul = max(0, 180 * (1.0 - wear) + np.random.normal(0, 5))

    rows.append({
        "p_mean": p_mean,
        "p_std": p_std,
        "p_slope": p_slope,
        "i_mean": i_mean,
        "i_std": i_std,
        "i_slope": i_slope,
        "rpm_mean": rpm_mean,
        "rpm_std": rpm_std,
        "rpm_slope": rpm_slope,
        "target_rul": target_rul,
    })

df = pd.DataFrame(rows)
df.to_csv("train_features.csv", index=False)

print(df.shape)
print(df.head())
print("Saved train_features.csv")