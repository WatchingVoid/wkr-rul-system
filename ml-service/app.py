from fastapi import FastAPI
from pydantic import BaseModel
import os, json
import numpy as np
from xgboost import XGBRegressor

app = FastAPI()

MODEL_PATH = os.getenv("MODEL_PATH", "/app/model.json")
MODEL_VERSION = os.getenv("MODEL_VERSION", "xgb_external")
METRICS_PATH = os.getenv("METRICS_PATH", "/app/metrics.json")

model = XGBRegressor()
if os.path.exists(MODEL_PATH):
    model.load_model(MODEL_PATH)
else:
    # если модели нет — лучше падать, чтобы ты сразу понял
    raise RuntimeError(f"Model file not found: {MODEL_PATH}")

class PredictRequest(BaseModel):
    features: dict[str, float]

@app.get("/health")
def health():
    return {"ok": True, "model_version": MODEL_VERSION}

@app.get("/model/info")
def model_info():
    if os.path.exists(METRICS_PATH):
        return json.load(open(METRICS_PATH, "r", encoding="utf-8"))
    return {"model_version": MODEL_VERSION, "note": "metrics.json not found"}

@app.post("/predict")
def predict(req: PredictRequest):
    keys = sorted(req.features.keys())
    X = np.array([[req.features[k] for k in keys]], dtype=float)

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