from fastapi import FastAPI
from pydantic import BaseModel
import numpy as np
import joblib
import os

from sklearn.linear_model import LinearRegression
from features import calc_features

MODEL_PATH = "model.joblib"

app = FastAPI()

def ensure_model():
    if os.path.exists(MODEL_PATH):
        return joblib.load(MODEL_PATH)

    # Заглушка: учим на синтетике, чтобы сервис ожил.
    rng = np.random.default_rng(42)
    X = rng.normal(size=(200, 7))
    # Сделаем "RUL" условно убывающим от мощности/тока
    y = 120 - 25*X[:, 0] - 15*X[:, 3] + rng.normal(scale=5, size=200)
    y = np.clip(y, 1, 180)

    m = LinearRegression()
    m.fit(X, y)
    joblib.dump(m, MODEL_PATH)
    return m

model = ensure_model()

class Window(BaseModel):
    power: list[float]
    current: list[float]
    rpm: list[float]

@app.get("/health")
def health():
    return {"ok": True}

@app.post("/predict")
def predict(w: Window):
    power = np.array(w.power, dtype=float)
    current = np.array(w.current, dtype=float)
    rpm = np.array(w.rpm, dtype=float)

    f = calc_features(power, current, rpm)
    keys = sorted(f.keys())  # фиксированный порядок
    X = np.array([[f[k] for k in keys]], dtype=float)

    rul = float(model.predict(X)[0])
    rul = float(np.clip(rul, 1, 500))

    alarm = 0
    if rul < 15:
        alarm = 2
    elif rul < 60:
        alarm = 1

    return {"rul_minutes": rul, "alarm_level": alarm, "features": {k: float(f[k]) for k in keys}}