alter table wkr.rul_predictions
add column if not exists alarm_code text null,
add column if not exists state text null,
add column if not exists message text null,
add column if not exists required_action text null,
add column if not exists features_json jsonb null,
add column if not exists explanation_json jsonb null;

create table if not exists wkr.alarm_events (
  id bigserial primary key,
  ts timestamptz not null,
  machine_id text not null,
  tool_id text not null,

  rul_minutes real not null,
  alarm_level int not null,

  alarm_code text not null,
  alarm_message text not null,
  required_action text not null,

  is_active boolean not null default true,
  model_version text not null
);

create index if not exists ix_alarm_events_machine_tool_time
on wkr.alarm_events(machine_id, tool_id, ts desc);

create index if not exists ix_alarm_events_active
on wkr.alarm_events(is_active, alarm_level, ts desc);

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
  effective_power_kw real,
  tangential_force_n real
)
language sql
as $$
  select
    x.ts,
    x.spindle_rpm,
    x.spindle_current_a,
    x.spindle_power_kw,
    x.cutting_speed_mmin,
    coalesce(x.power_from_torque_kw, x.spindle_power_kw) as effective_power_kw,
    x.tangential_force_n
  from (
    select t.*
    from wkr.telemetry_spindle t
    where t.machine_id = p_machine_id
      and t.tool_id = p_tool_id
      and t.cut_flag = true
    order by t.ts desc
    limit p_window_size
  ) x
  order by x.ts asc;
$$;

create or replace function wkr.insert_rul_prediction(
  p_ts timestamptz,
  p_machine_id text,
  p_tool_id text,
  p_rul_minutes real,
  p_alarm_level int,
  p_alarm_code text,
  p_state text,
  p_message text,
  p_required_action text,
  p_model_version text,
  p_features_json jsonb,
  p_explanation_json jsonb
) returns bigint
language sql
as $$
  insert into wkr.rul_predictions(
    ts,
    machine_id,
    tool_id,
    rul_minutes,
    alarm_level,
    alarm_code,
    state,
    message,
    required_action,
    model_version,
    features_json,
    explanation_json
  )
  values (
    p_ts,
    p_machine_id,
    p_tool_id,
    p_rul_minutes,
    p_alarm_level,
    p_alarm_code,
    p_state,
    p_message,
    p_required_action,
    p_model_version,
    p_features_json,
    p_explanation_json
  )
  returning id;
$$;

create or replace function wkr.get_last_rul(
  p_machine_id text,
  p_tool_id text
)
returns table(
  ts timestamptz,
  rul_minutes real,
  alarm_level int,
  alarm_code text,
  state text,
  message text,
  required_action text,
  model_version text
)
language sql
as $$
  select
    r.ts,
    r.rul_minutes,
    r.alarm_level,
    r.alarm_code,
    r.state,
    r.message,
    r.required_action,
    r.model_version
  from wkr.rul_predictions r
  where r.machine_id = p_machine_id
    and r.tool_id = p_tool_id
  order by r.ts desc
  limit 1;
$$;

create or replace function wkr.insert_alarm_event(
  p_ts timestamptz,
  p_machine_id text,
  p_tool_id text,
  p_rul_minutes real,
  p_alarm_level int,
  p_alarm_code text,
  p_alarm_message text,
  p_required_action text,
  p_model_version text
) returns bigint
language sql
as $$
  insert into wkr.alarm_events(
    ts,
    machine_id,
    tool_id,
    rul_minutes,
    alarm_level,
    alarm_code,
    alarm_message,
    required_action,
    model_version
  )
  values (
    p_ts,
    p_machine_id,
    p_tool_id,
    p_rul_minutes,
    p_alarm_level,
    p_alarm_code,
    p_alarm_message,
    p_required_action,
    p_model_version
  )
  returning id;
$$;

create or replace function wkr.get_last_alarm(
  p_machine_id text,
  p_tool_id text
)
returns table(
  ts timestamptz,
  machine_id text,
  tool_id text,
  rul_minutes real,
  alarm_level int,
  alarm_code text,
  alarm_message text,
  required_action text,
  is_active boolean,
  model_version text
)
language sql
as $$
  select
    a.ts,
    a.machine_id,
    a.tool_id,
    a.rul_minutes,
    a.alarm_level,
    a.alarm_code,
    a.alarm_message,
    a.required_action,
    a.is_active,
    a.model_version
  from wkr.alarm_events a
  where a.machine_id = p_machine_id
    and a.tool_id = p_tool_id
  order by a.ts desc
  limit 1;
$$;