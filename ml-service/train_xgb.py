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

def main():
    df = pd.read_csv(DATA)
    y = df["target_rul"].to_numpy(dtype=float)

    feature_cols = [c for c in df.columns if c != "target_rul"]
    X = df[feature_cols].to_numpy(dtype=float)

    Xtr, Xte, ytr, yte = train_test_split(X, y, test_size=0.2, random_state=42)

    model = XGBRegressor(
        n_estimators=800,
        max_depth=6,
        learning_rate=0.03,
        subsample=0.9,
        colsample_bytree=0.9,
        reg_lambda=1.0,
        random_state=42,
        n_jobs=4
    )
    model.fit(Xtr, ytr)

    yp = model.predict(Xte)
    mae = float(mean_absolute_error(yte, yp))
    rmse = float(np.sqrt(mean_squared_error(yte, yp)))
    r2 = float(r2_score(yte, yp))

    model.save_model(MODEL_OUT)

    metrics = {
        "model_version": "xgb_rul_v1",
        "trained_at": datetime.now(timezone.utc).isoformat(),
        "mae": mae, "rmse": rmse, "r2": r2,
        "features": feature_cols,
        "n_train": int(len(ytr)),
        "n_test": int(len(yte))
    }
    with open(METRICS_OUT, "w", encoding="utf-8") as f:
        json.dump(metrics, f, ensure_ascii=False, indent=2)

    print(metrics)

if __name__ == "__main__":
    main()