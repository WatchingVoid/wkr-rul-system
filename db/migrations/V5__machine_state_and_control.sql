-- V5__machine_state_and_control.sql

-- 1. Расширяем телеметрию состоянием станка и управляющим действием

alter table wkr.telemetry_spindle
  add column if not exists machine_state text not null default 'unknown',
  add column if not exists spindle_state text not null default 'unknown',
  add column if not exists stop_required boolean not null default false,
  add column if not exists stop_reason text null,
  add column if not exists control_action text null;

create index if not exists ix_tel_machine_state_time
  on wkr.telemetry_spindle(machine_id, machine_state, ts desc);

create index if not exists ix_tel_stop_required_time
  on wkr.telemetry_spindle(stop_required, ts desc);

-- 2. Таблица событий станка

create table if not exists wkr.machine_events (
  id bigserial primary key,
  ts timestamptz not null,
  machine_id text not null,
  tool_id text null,

  event_code text not null,
  event_level int not null default 0,
  event_message text not null,

  machine_state text null,
  spindle_state text null,
  stop_reason text null,
  control_action text null
);

create index if not exists ix_machine_events_machine_time
  on wkr.machine_events(machine_id, ts desc);

create index if not exists ix_machine_events_code_time
  on wkr.machine_events(event_code, ts desc);

-- 3. Новая версия функции вставки телеметрии.
-- Старую функцию не удаляем, чтобы не ломать совместимость.

create or replace function wkr.insert_telemetry_spindle_v2(
  p_ts timestamptz,
  p_machine_id text,
  p_tool_id text,
  p_spindle_rpm int,
  p_spindle_current_a real,
  p_spindle_power_kw real,
  p_feed_mm_min int,
  p_program text,
  p_cut_flag boolean,
  p_tool_diameter_mm real,
  p_cutting_speed_mmin real,
  p_power_from_torque_kw real,
  p_tangential_force_n real,
  p_machine_state text,
  p_spindle_state text,
  p_stop_required boolean,
  p_stop_reason text,
  p_control_action text
) returns bigint
language sql
as $$
  insert into wkr.telemetry_spindle(
    ts,
    machine_id,
    tool_id,
    spindle_rpm,
    spindle_current_a,
    spindle_power_kw,
    feed_mm_min,
    program,
    cut_flag,
    tool_diameter_mm,
    cutting_speed_mmin,
    power_from_torque_kw,
    tangential_force_n,
    machine_state,
    spindle_state,
    stop_required,
    stop_reason,
    control_action
  )
  values (
    p_ts,
    p_machine_id,
    p_tool_id,
    p_spindle_rpm,
    p_spindle_current_a,
    p_spindle_power_kw,
    p_feed_mm_min,
    p_program,
    p_cut_flag,
    p_tool_diameter_mm,
    p_cutting_speed_mmin,
    p_power_from_torque_kw,
    p_tangential_force_n,
    coalesce(p_machine_state, 'unknown'),
    coalesce(p_spindle_state, 'unknown'),
    coalesce(p_stop_required, false),
    p_stop_reason,
    p_control_action
  )
  returning id;
$$;

-- 4. Вставка события станка

create or replace function wkr.insert_machine_event(
  p_ts timestamptz,
  p_machine_id text,
  p_tool_id text,
  p_event_code text,
  p_event_level int,
  p_event_message text,
  p_machine_state text,
  p_spindle_state text,
  p_stop_reason text,
  p_control_action text
) returns bigint
language sql
as $$
  insert into wkr.machine_events(
    ts,
    machine_id,
    tool_id,
    event_code,
    event_level,
    event_message,
    machine_state,
    spindle_state,
    stop_reason,
    control_action
  )
  values (
    p_ts,
    p_machine_id,
    p_tool_id,
    p_event_code,
    p_event_level,
    p_event_message,
    p_machine_state,
    p_spindle_state,
    p_stop_reason,
    p_control_action
  )
  returning id;
$$;

-- 5. Последнее состояние станка

create or replace function wkr.get_last_machine_state(
  p_machine_id text
)
returns table(
  ts timestamptz,
  machine_id text,
  tool_id text,
  spindle_rpm int,
  spindle_current_a real,
  spindle_power_kw real,
  feed_mm_min int,
  program text,
  cut_flag boolean,
  machine_state text,
  spindle_state text,
  stop_required boolean,
  stop_reason text,
  control_action text
)
language sql
as $$
  select
    t.ts,
    t.machine_id,
    t.tool_id,
    t.spindle_rpm,
    t.spindle_current_a,
    t.spindle_power_kw,
    t.feed_mm_min,
    t.program,
    t.cut_flag,
    t.machine_state,
    t.spindle_state,
    t.stop_required,
    t.stop_reason,
    t.control_action
  from wkr.telemetry_spindle t
  where t.machine_id = p_machine_id
  order by t.ts desc
  limit 1
$$;

-- 6. Последние события станка

create or replace function wkr.get_machine_events(
  p_machine_id text,
  p_limit int
)
returns table(
  id bigint,
  ts timestamptz,
  machine_id text,
  tool_id text,
  event_code text,
  event_level int,
  event_message text,
  machine_state text,
  spindle_state text,
  stop_reason text,
  control_action text
)
language sql
as $$
  select
    e.id,
    e.ts,
    e.machine_id,
    e.tool_id,
    e.event_code,
    e.event_level,
    e.event_message,
    e.machine_state,
    e.spindle_state,
    e.stop_reason,
    e.control_action
  from wkr.machine_events e
  where e.machine_id = p_machine_id
  order by e.ts desc
  limit greatest(1, least(p_limit, 100))
$$;