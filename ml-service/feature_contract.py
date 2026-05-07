from typing import Any
import math
import numpy as np


FEATURE_ORDER = [
    # Исходная телеметрия / нагрузка
    "p_mean",
    "p_std",
    "p_slope",

    "i_mean",
    "i_std",
    "i_slope",

    "rpm_mean",
    "rpm_std",
    "rpm_slope",

    # Производные величины, рассчитанные в backend/CuttingMath.cs
    "v_mean",
    "v_std",
    "v_slope",

    "ne_mean",
    "ne_std",
    "ne_slope",

    "pz_mean",
    "pz_std",
    "pz_slope",
]


FORMULA_FEATURES_DESCRIPTION = {
    "v": "Рассчитывается в backend по формуле v = π·D·n/1000",
    "ne": "Рассчитывается в backend по формуле Ne = M·n/9550 либо принимается по мощности шпинделя, если момент недоступен",
    "pz": "Рассчитывается в backend по формуле Pz = 60000·Ne/v",
}


def validate_features(
    features: dict[str, Any],
    expected_order: list[str],
) -> dict[str, float]:
    missing = [name for name in expected_order if name not in features]
    extra = [name for name in features.keys() if name not in expected_order]

    if missing:
        raise ValueError(f"Missing features: {missing}")

    if extra:
        raise ValueError(f"Extra features: {extra}")

    result: dict[str, float] = {}

    for name in expected_order:
        value = features[name]

        if value is None:
            value = 0.0

        value = float(value)

        if math.isnan(value) or math.isinf(value):
            value = 0.0

        result[name] = value

    return result


def to_vector(features: dict[str, float], feature_order: list[str]) -> np.ndarray:
    clean = validate_features(features, feature_order)
    return np.array([[clean[name] for name in feature_order]], dtype=float)