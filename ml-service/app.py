import json
import os
from typing import Any

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from xgboost import XGBRegressor

from feature_contract import (
    FEATURE_ORDER,
    FORMULA_FEATURES_DESCRIPTION,
    to_vector,
    validate_features,
)


app = FastAPI(
    title="WKR RUL ML Service",
    description="XGBoost service for cutting tool remaining useful life prediction",
    version="3.0.0",
)

MODEL_PATH = os.getenv("MODEL_PATH", "/app/model.json")
METRICS_PATH = os.getenv("METRICS_PATH", "/app/metrics.json")
MODEL_VERSION_ENV = os.getenv("MODEL_VERSION", "xgb_rul_v3_formula_features")

WARN_THRESHOLD_MINUTES = float(os.getenv("RUL_WARN_MINUTES", "60"))
STOP_THRESHOLD_MINUTES = float(os.getenv("RUL_STOP_MINUTES", "15"))

model = XGBRegressor()


def load_metrics() -> dict[str, Any]:
    if not os.path.exists(METRICS_PATH):
        return {
            "model_version": MODEL_VERSION_ENV,
            "features": FEATURE_ORDER,
            "note": "metrics.json not found",
        }

    with open(METRICS_PATH, "r", encoding="utf-8") as f:
        return json.load(f)


METRICS = load_metrics()
MODEL_VERSION = METRICS.get("model_version", MODEL_VERSION_ENV)

MODEL_FEATURE_ORDER = METRICS.get("features", FEATURE_ORDER)

if not isinstance(MODEL_FEATURE_ORDER, list) or len(MODEL_FEATURE_ORDER) == 0:
    MODEL_FEATURE_ORDER = FEATURE_ORDER

if os.path.exists(MODEL_PATH):
    model.load_model(MODEL_PATH)
else:
    raise RuntimeError(f"Model file not found: {MODEL_PATH}")


class PredictRequest(BaseModel):
    features: dict[str, float] = Field(
        description="Feature vector prepared by Backend FeatureExtractor"
    )


class PredictResponse(BaseModel):
    rulMinutes: float
    alarmLevel: int
    alarmCode: str
    state: str
    message: str
    requiredAction: str
    modelVersion: str
    explanation: list[str]
    usedFeatures: dict[str, float]


def build_alarm(rul_minutes: float) -> tuple[int, str, str, str, str]:
    if rul_minutes <= STOP_THRESHOLD_MINUTES:
        return (
            2,
            "TOOL_RUL_STOP",
            "critical",
            f"Остаточный ресурс инструмента критически мал: {rul_minutes:.1f} мин.",
            "Остановить обработку или заблокировать запуск шпинделя до замены инструмента.",
        )

    if rul_minutes <= WARN_THRESHOLD_MINUTES:
        return (
            1,
            "TOOL_RUL_WARNING",
            "warning",
            f"Остаточный ресурс инструмента снижается: {rul_minutes:.1f} мин.",
            "Предупредить оператора и подготовить замену инструмента.",
        )

    return (
        0,
        "TOOL_RUL_OK",
        "normal",
        f"Остаточный ресурс инструмента в допустимом диапазоне: {rul_minutes:.1f} мин.",
        "Продолжить обработку.",
    )


def build_explanation(features: dict[str, float], rul_minutes: float) -> list[str]:
    explanation: list[str] = []

    p_mean = features.get("p_mean", 0.0)
    p_slope = features.get("p_slope", 0.0)

    i_mean = features.get("i_mean", 0.0)
    i_slope = features.get("i_slope", 0.0)

    rpm_mean = features.get("rpm_mean", 0.0)

    v_mean = features.get("v_mean", 0.0)
    ne_mean = features.get("ne_mean", 0.0)

    pz_mean = features.get("pz_mean", 0.0)
    pz_slope = features.get("pz_slope", 0.0)

    if p_mean > 0:
        explanation.append(
            f"Средняя мощность шпинделя p_mean={p_mean:.2f} кВт используется как показатель энергетической нагрузки процесса."
        )

    if p_slope > 0:
        explanation.append(
            f"Тренд мощности p_slope={p_slope:.4f} положительный, что может указывать на рост сопротивления резанию."
        )

    if i_mean > 0:
        explanation.append(
            f"Средний ток шпинделя i_mean={i_mean:.2f} А используется как дополнительный признак нагрузки электропривода."
        )

    if i_slope > 0:
        explanation.append(
            f"Тренд тока i_slope={i_slope:.4f} положительный, что соответствует увеличению нагрузки на привод."
        )

    if rpm_mean > 0:
        explanation.append(
            f"Средняя частота вращения rpm_mean={rpm_mean:.1f} об/мин учитывается для сопоставимости режима резания."
        )

    if v_mean > 0:
        explanation.append(
            f"Средняя скорость резания v_mean={v_mean:.1f} м/мин получена из backend как расчётный признак по формуле v = π·D·n/1000."
        )

    if ne_mean > 0:
        explanation.append(
            f"Средняя эффективная мощность ne_mean={ne_mean:.2f} кВт получена из backend как расчётный признак мощности резания."
        )

    if pz_mean > 0:
        explanation.append(
            f"Средняя касательная сила резания pz_mean={pz_mean:.1f} Н получена из backend как расчётный признак по формуле Pz = 60000·Ne/v."
        )

    if pz_slope > 0:
        explanation.append(
            f"Тренд касательной силы pz_slope={pz_slope:.4f} положительный, что является диагностическим признаком роста сил резания."
        )

    if rul_minutes <= STOP_THRESHOLD_MINUTES:
        explanation.append(
            "Прогноз RUL ниже критического порога, поэтому сформирован статус TOOL_RUL_STOP."
        )
    elif rul_minutes <= WARN_THRESHOLD_MINUTES:
        explanation.append(
            "Прогноз RUL ниже предупредительного порога, поэтому сформирован статус TOOL_RUL_WARNING."
        )
    else:
        explanation.append(
            "Прогноз RUL выше предупредительного порога, состояние инструмента считается допустимым."
        )

    return explanation


@app.get("/health")
def health():
    return {
        "ok": True,
        "model_version": MODEL_VERSION,
        "feature_count": len(MODEL_FEATURE_ORDER),
        "features": MODEL_FEATURE_ORDER,
        "warn_threshold_minutes": WARN_THRESHOLD_MINUTES,
        "stop_threshold_minutes": STOP_THRESHOLD_MINUTES,
    }


@app.get("/model/info")
def model_info():
    return {
        "model_version": MODEL_VERSION,
        "model_path": MODEL_PATH,
        "metrics_path": METRICS_PATH,
        "feature_count": len(MODEL_FEATURE_ORDER),
        "features": MODEL_FEATURE_ORDER,
        "metrics": METRICS,
        "warning_logic": {
            "normal": f"RUL > {WARN_THRESHOLD_MINUTES} min",
            "warning": f"{STOP_THRESHOLD_MINUTES} < RUL <= {WARN_THRESHOLD_MINUTES} min",
            "critical": f"RUL <= {STOP_THRESHOLD_MINUTES} min",
        },
        "formula_features": FORMULA_FEATURES_DESCRIPTION,
        "runtime_note": (
            "ML-service не рассчитывает v, Ne, Pz в рабочем контуре. "
            "Эти величины должны быть рассчитаны в Backend/CuttingMath.cs и переданы как признаки."
        ),
    }


@app.post("/predict", response_model=PredictResponse)
def predict(req: PredictRequest):
    try:
        clean_features = validate_features(req.features, MODEL_FEATURE_ORDER)
        x = to_vector(clean_features, MODEL_FEATURE_ORDER)
    except ValueError as ex:
        raise HTTPException(
            status_code=400,
            detail={
                "error": "invalid_features",
                "message": str(ex),
                "expected": MODEL_FEATURE_ORDER,
                "received": list(req.features.keys()),
            },
        )

    try:
        rul = float(model.predict(x)[0])
    except Exception as ex:
        raise HTTPException(
            status_code=500,
            detail={
                "error": "model_prediction_failed",
                "message": str(ex),
                "expected_feature_count": len(MODEL_FEATURE_ORDER),
                "received_feature_count": len(clean_features),
            },
        )

    rul = max(0.0, rul)

    alarm_level, alarm_code, state, message, required_action = build_alarm(rul)
    explanation = build_explanation(clean_features, rul)

    return PredictResponse(
        rulMinutes=rul,
        alarmLevel=alarm_level,
        alarmCode=alarm_code,
        state=state,
        message=message,
        requiredAction=required_action,
        modelVersion=MODEL_VERSION,
        explanation=explanation,
        usedFeatures=clean_features,
    )