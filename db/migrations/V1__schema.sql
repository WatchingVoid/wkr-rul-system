-- db/migrations/V1__schema.sql
create schema if not exists wkr;

-- Таблица телеметрии шпинделя (сырьё + место под производные)
create table if not exists wkr.telemetry_spindle (
  id bigserial primary key,
  ts timestamptz not null,
  machine_id text not null,
  tool_id text not null,

  spindle_rpm int not null,
  spindle_current_a real not null,
  spindle_power_kw real not null,
  feed_mm_min int not null,
  program text null,
  cut_flag boolean not null default false,

  -- параметры для формул главы 1 (D и производные)
  tool_diameter_mm real null,
  cutting_speed_mmin real null,       -- v
  power_from_torque_kw real null,     -- Ne (если считаешь по моменту)
  tangential_force_n real null        -- Pz
);

create index if not exists ix_tel_time on wkr.telemetry_spindle (ts desc);
create index if not exists ix_tel_machine_tool_time on wkr.telemetry_spindle (machine_id, tool_id, ts desc);
create index if not exists ix_tel_cut on wkr.telemetry_spindle (cut_flag, ts desc);

-- Прогнозы RUL
create table if not exists wkr.rul_predictions (
  id bigserial primary key,
  ts timestamptz not null,
  machine_id text not null,
  tool_id text not null,
  rul_minutes real not null,
  alarm_level int not null,
  model_version text not null
);

create index if not exists ix_rul_machine_tool_time on wkr.rul_predictions (machine_id, tool_id, ts desc);

-- Справочник инструмента (минимум: диаметр)
create table if not exists wkr.tools (
  tool_id text primary key,
  diameter_mm real not null check (diameter_mm > 0)
);