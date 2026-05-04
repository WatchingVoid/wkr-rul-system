from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import os
import json
import numpy as np
from xgboost import XGBRegressor

app = FastAPI()

MODEL_PATH = os.getenv("MODEL_PATH", "/app/model.json")
MODEL_VERSION = os.getenv("MODEL_VERSION", "xgb_external")
METRICS_PATH = os.getenv("METRICS_PATH", "/app/metrics.json")

DEFAULT_FEATURE_ORDER = [
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

model = XGBRegressor()

if os.path.exists(MODEL_PATH):
    model.load_model(MODEL_PATH)
else:
    raise RuntimeError(f"Model file not found: {MODEL_PATH}")

def load_feature_order() -> list[str]:
    if os.path.exists(METRICS_PATH):
        with open(METRICS_PATH, "r", encoding="utf-8") as f:
            metrics = json.load(f)

        features = metrics.get("features")
        if isinstance(features, list) and len(features) > 0:
            return features

    return DEFAULT_FEATURE_ORDER

FEATURE_ORDER = load_feature_order()

class PredictRequest(BaseModel):
    features: dict[str, float]

@app.get("/health")
def health():
    return {
        "ok": True,
        "model_version": MODEL_VERSION,
        "feature_count": len(FEATURE_ORDER),
        "features": FEATURE_ORDER,
    }

@app.get("/model/info")
def model_info():
    result = {
        "model_version": MODEL_VERSION,
        "features": FEATURE_ORDER,
    }

    if os.path.exists(METRICS_PATH):
        with open(METRICS_PATH, "r", encoding="utf-8") as f:
            result["metrics"] = json.load(f)

    return result

@app.post("/predict")
def predict(req: PredictRequest):
    missing = [name for name in FEATURE_ORDER if name not in req.features]
    extra = [name for name in req.features.keys() if name not in FEATURE_ORDER]

    if missing:
        raise HTTPException(
            status_code=400,
            detail={
                "error": "missing_features",
                "missing": missing,
                "expected": FEATURE_ORDER,
                "received": list(req.features.keys()),
            },
        )

    if extra:
        raise HTTPException(
            status_code=400,
            detail={
                "error": "extra_features",
                "extra": extra,
                "expected": FEATURE_ORDER,
                "received": list(req.features.keys()),
            },
        )

    X = np.array([[req.features[name] for name in FEATURE_ORDER]], dtype=float)

    rul = float(model.predict(X)[0])

    alarm = 0
    if rul < 15:
        alarm = 2
    elif rul < 60:
        alarm = 1

    return {
        "rulMinutes": rul,
        "alarmLevel": alarm,
        "modelVersion": MODEL_VERSION
    }