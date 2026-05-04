import numpy as np

def calc_features(power: np.ndarray, current: np.ndarray, rpm: np.ndarray) -> dict:
    def slope(x: np.ndarray) -> float:
        if x.size < 2:
            return 0.0
        t = np.arange(x.size, dtype=float)
        return float(np.polyfit(t, x, 1)[0])

    return {
        "p_mean": float(np.mean(power)),
        "p_std": float(np.std(power)),
        "p_slope": slope(power),

        "i_mean": float(np.mean(current)),
        "i_std": float(np.std(current)),
        "i_slope": slope(current),

        "rpm_mean": float(np.mean(rpm)),
        "rpm_std": float(np.std(rpm)),
        "rpm_slope": slope(rpm),
    }