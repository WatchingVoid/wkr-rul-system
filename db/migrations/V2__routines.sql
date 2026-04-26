-- db/migrations/V2__routines.sql

-- upsert инструмента (на случай если Collector/Backend присылает toolId + D)
create or replace function wkr.upsert_tool(p_tool_id text, p_diameter_mm real)
returns void
language plpgsql
as $$
begin
  insert into wkr.tools(tool_id, diameter_mm)
  values (p_tool_id, p_diameter_mm)
  on conflict (tool_id) do update set diameter_mm = excluded.diameter_mm;
end;
$$;

-- вставка телеметрии (возвращаем id)
create or replace function wkr.insert_telemetry_spindle(
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
  p_tangential_force_n real
) returns bigint
language sql
as $$
  insert into wkr.telemetry_spindle(
    ts, machine_id, tool_id,
    spindle_rpm, spindle_current_a, spindle_power_kw, feed_mm_min, program, cut_flag,
    tool_diameter_mm, cutting_speed_mmin, power_from_torque_kw, tangential_force_n
  )
  values (
    p_ts, p_machine_id, p_tool_id,
    p_spindle_rpm, p_spindle_current_a, p_spindle_power_kw, p_feed_mm_min, p_program, p_cut_flag,
    p_tool_diameter_mm, p_cutting_speed_mmin, p_power_from_torque_kw, p_tangential_force_n
  )
  returning id;
$$;

-- окно резания для ML (берём только cut_flag=true, упорядочиваем по времени ВПЕРЁД)
create or replace function wkr.select_cut_window(
  p_machine_id text,
  p_tool_id text,
  p_window_size int
)
returns table(
  ts timestamptz,
  spindle_rpm int,
  spindle_current_a real,
  spindle_power_kw real,
  cutting_speed_mmin real,
  tangential_force_n real
)
language sql
as $$
  select t.ts, t.spindle_rpm, t.spindle_current_a, t.spindle_power_kw, t.cutting_speed_mmin, t.tangential_force_n
  from wkr.telemetry_spindle t
  where t.machine_id = p_machine_id
    and t.tool_id = p_tool_id
    and t.cut_flag = true
  order by t.ts desc
  limit p_window_size
$$;

-- вставка прогноза RUL
create or replace function wkr.insert_rul_prediction(
  p_ts timestamptz,
  p_machine_id text,
  p_tool_id text,
  p_rul_minutes real,
  p_alarm_level int,
  p_model_version text
) returns bigint
language sql
as $$
  insert into wkr.rul_predictions(ts, machine_id, tool_id, rul_minutes, alarm_level, model_version)
  values (p_ts, p_machine_id, p_tool_id, p_rul_minutes, p_alarm_level, p_model_version)
  returning id;
$$;

-- последний прогноз
create or replace function wkr.get_last_rul(p_machine_id text, p_tool_id text)
returns table(ts timestamptz, rul_minutes real, alarm_level int, model_version text)
language sql
as $$
  select r.ts, r.rul_minutes, r.alarm_level, r.model_version
  from wkr.rul_predictions r
  where r.machine_id = p_machine_id and r.tool_id = p_tool_id
  order by r.ts desc
  limit 1
$$;