import json
import numpy as np
import pandas as pd
from datetime import datetime, timezone
from xgboost import XGBRegressor
from sklearn.model_selection import train_test_split
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score

DATA = "train_features.csv"
MODEL_OUT = "model.json"
METRICS_OUT = "metrics.json"

FEATURE_COLS = [
    "p_mean",
    "p_std",
    "p_slope",
    "i_mean",
    "i_std",
    "i_slope",
    "rpm_mean",
    "rpm_std",
    "rpm_slope",
]

TARGET_COL = "target_rul"

def main():
    df = pd.read_csv(DATA)

    print("Loaded:", DATA)
    print("Shape:", df.shape)
    print("Columns:", df.columns.tolist())

    if df.empty:
        raise RuntimeError("train_features.csv is empty: no rows for training")

    required = FEATURE_COLS + [TARGET_COL]
    missing = [c for c in required if c not in df.columns]
    if missing:
        raise RuntimeError(f"Missing columns in train_features.csv: {missing}")

    df = df[required].dropna()

    if len(df) < 10:
        raise RuntimeError(f"Too few rows for training: {len(df)}. Need at least 10.")

    X = df[FEATURE_COLS].to_numpy(dtype=float)
    y = df[TARGET_COL].to_numpy(dtype=float)

    Xtr, Xte, ytr, yte = train_test_split(
        X,
        y,
        test_size=0.2,
        random_state=42
    )

    model = XGBRegressor(
        n_estimators=500,
        max_depth=4,
        learning_rate=0.04,
        subsample=0.9,
        colsample_bytree=0.9,
        reg_lambda=1.0,
        random_state=42,
        n_jobs=4,
        objective="reg:squarederror"
    )

    model.fit(Xtr, ytr)

    yp = model.predict(Xte)

    mae = float(mean_absolute_error(yte, yp))
    rmse = float(np.sqrt(mean_squared_error(yte, yp)))
    r2 = float(r2_score(yte, yp))

    model.save_model(MODEL_OUT)

    metrics = {
        "model_version": "xgb_rul_v2_9features",
        "trained_at": datetime.now(timezone.utc).isoformat(),
        "mae": mae,
        "rmse": rmse,
        "r2": r2,
        "features": FEATURE_COLS,
        "n_train": int(len(ytr)),
        "n_test": int(len(yte)),
        "n_total": int(len(df))
    }

    with open(METRICS_OUT, "w", encoding="utf-8") as f:
        json.dump(metrics, f, ensure_ascii=False, indent=2)

    print(json.dumps(metrics, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()