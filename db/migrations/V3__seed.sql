-- db/migrations/V3__seed.sql
-- Пример: тестовый инструмент, чтобы сразу работали формулы v (если tool_diameter_mm не приходит в телеметрии)
select wkr.upsert_tool('T12', 10.0);