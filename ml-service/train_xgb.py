import json
from datetime import datetime, timezone

import numpy as np
import pandas as pd
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score
from sklearn.model_selection import GroupShuffleSplit, train_test_split
from xgboost import XGBRegressor

from feature_contract import FEATURE_ORDER, FORMULA_FEATURES_DESCRIPTION


DATA = "train_features.csv"
MODEL_OUT = "model.json"
METRICS_OUT = "metrics.json"
TARGET_COL = "target_rul"


def split_data(df: pd.DataFrame):
    x = df[FEATURE_ORDER].to_numpy(dtype=float)
    y = df[TARGET_COL].to_numpy(dtype=float)

    if "life_id" in df.columns and df["life_id"].nunique() >= 3:
        groups = df["life_id"].to_numpy()

        splitter = GroupShuffleSplit(
            n_splits=1,
            test_size=0.2,
            random_state=42,
        )

        train_idx, test_idx = next(splitter.split(x, y, groups=groups))

        return (
            x[train_idx],
            x[test_idx],
            y[train_idx],
            y[test_idx],
            "GroupShuffleSplit by life_id",
        )

    xtr, xte, ytr, yte = train_test_split(
        x,
        y,
        test_size=0.2,
        random_state=42,
        shuffle=True,
    )

    return xtr, xte, ytr, yte, "train_test_split"


def main():
    df = pd.read_csv(DATA)

    print("Loaded:", DATA)
    print("Shape:", df.shape)
    print("Columns:", df.columns.tolist())

    if df.empty:
        raise RuntimeError("train_features.csv is empty: no rows for training")

    required = FEATURE_ORDER + [TARGET_COL]
    missing = [c for c in required if c not in df.columns]

    if missing:
        raise RuntimeError(f"Missing columns in train_features.csv: {missing}")

    df = df.dropna(subset=required)

    if len(df) < 20:
        raise RuntimeError(f"Too few rows for training: {len(df)}. Need at least 20.")

    xtr, xte, ytr, yte, split_method = split_data(df)

    model = XGBRegressor(
        n_estimators=700,
        max_depth=4,
        learning_rate=0.04,
        subsample=0.9,
        colsample_bytree=0.9,
        reg_lambda=1.0,
        random_state=42,
        n_jobs=4,
        objective="reg:squarederror",
    )

    model.fit(xtr, ytr)

    yp = model.predict(xte)

    mae = float(mean_absolute_error(yte, yp))
    rmse = float(np.sqrt(mean_squared_error(yte, yp)))
    r2 = float(r2_score(yte, yp))

    model.save_model(MODEL_OUT)

    importance = {
        name: float(value)
        for name, value in zip(FEATURE_ORDER, model.feature_importances_)
    }

    metrics = {
        "model_version": "xgb_rul_v3_formula_features",
        "trained_at": datetime.now(timezone.utc).isoformat(),
        "model_type": "XGBRegressor",
        "target": TARGET_COL,
        "features": FEATURE_ORDER,
        "feature_count": len(FEATURE_ORDER),
        "n_train": int(len(ytr)),
        "n_test": int(len(yte)),
        "n_total": int(len(df)),
        "split_method": split_method,
        "mae": mae,
        "rmse": rmse,
        "r2": r2,
        "feature_importance": importance,
        "formula_features": FORMULA_FEATURES_DESCRIPTION,
        "description": (
            "XGBoost regression model for cutting tool RUL prediction. "
            "The model consumes a feature vector prepared by Backend. "
            "Formula-based features v, Ne and Pz are calculated outside ML-service."
        ),
    }


    with open(METRICS_OUT, "w", encoding="utf-8") as f:
        json.dump(metrics, f, ensure_ascii=False, indent=2)

    eval_df = pd.DataFrame({
        "y_true": yte,
        "y_pred": yp,
        "abs_error": np.abs(yte - yp),
    })
    eval_df.to_csv("eval_predictions.csv", index=False)

    print(json.dumps(metrics, ensure_ascii=False, indent=2))
    print("Saved:", MODEL_OUT)
    print("Saved:", METRICS_OUT)
    print("Saved: eval_predictions.csv")


if __name__ == "__main__":
    main()