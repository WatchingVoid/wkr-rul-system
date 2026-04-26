from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import numpy as np
import joblib
import os

from features import calc_features

MODEL_PATH = os.getenv("MODEL_PATH", "model.joblib")
MODEL_VERSION = os.getenv("MODEL_VERSION", "external")

app = FastAPI()


def ensure_model():
    if os.path.exists(MODEL_PATH):
        return joblib.load(MODEL_PATH)
    return None


model = ensure_model()


class Window(BaseModel):
    power: list[float]
    current: list[float]
    rpm: list[float]


@app.get("/health")
def health():
    loaded = model is not None
    if not loaded:
        raise HTTPException(status_code=503, detail="Model artifact is not loaded")
    return {"ok": True, "model_loaded": True, "model_version": MODEL_VERSION}


@app.post("/predict")
def predict(w: Window):
    if model is None:
        raise HTTPException(status_code=503, detail="Model artifact is not loaded")

    power = np.array(w.power, dtype=float)
    current = np.array(w.current, dtype=float)
    rpm = np.array(w.rpm, dtype=float)

    if power.size == 0 or current.size == 0 or rpm.size == 0:
        raise HTTPException(status_code=400, detail="Input window must not be empty")

    if not (power.size == current.size == rpm.size):
        raise HTTPException(status_code=400, detail="Input series lengths must match")

    f = calc_features(power, current, rpm)
    keys = sorted(f.keys())
    X = np.array([[f[k] for k in keys]], dtype=float)

    rul = float(model.predict(X)[0])
    rul = float(np.clip(rul, 1, 500))

    alarm = 0
    if rul < 15:
        alarm = 2
    elif rul < 60:
        alarm = 1

    return {
        "rul_minutes": rul,
        "alarm_level": alarm,
        "model_version": MODEL_VERSION,
        "features": {k: float(f[k]) for k in keys},
    }
