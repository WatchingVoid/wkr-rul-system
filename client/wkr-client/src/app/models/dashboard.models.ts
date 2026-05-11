export interface MachineStateDto {
  ts?: string;
  machineId?: string;
  machine_id?: string;
  toolId?: string;
  tool_id?: string;

  spindleRpm?: number;
  spindle_rpm?: number;

  spindleCurrentA?: number;
  spindle_current_a?: number;

  spindlePowerKw?: number;
  spindle_power_kw?: number;

  feedMmMin?: number;
  feed_mm_min?: number;

  program?: string;
  cutFlag?: boolean;
  cut_flag?: boolean;

  machineState?: string;
  machine_state?: string;

  spindleState?: string;
  spindle_state?: string;

  stopRequired?: boolean;
  stop_required?: boolean;

  stopReason?: string;
  stop_reason?: string;

  controlAction?: string;
  control_action?: string;
}

export interface RulPredictionDto {
  ts?: string;
  rulMinutes?: number;
  rul_minutes?: number;
  alarmLevel?: number;
  alarm_level?: number;
  alarmCode?: string;
  alarm_code?: string;
  state?: string;
  message?: string;
  requiredAction?: string;
  required_action?: string;
  modelVersion?: string;
  model_version?: string;
}

export interface MachineEventDto {
  id?: number;
  ts?: string;
  machineId?: string;
  machine_id?: string;
  toolId?: string;
  tool_id?: string;
  eventCode?: string;
  event_code?: string;
  eventLevel?: number;
  event_level?: number;
  eventMessage?: string;
  event_message?: string;
  machineState?: string;
  machine_state?: string;
  spindleState?: string;
  spindle_state?: string;
  stopReason?: string;
  stop_reason?: string;
  controlAction?: string;
  control_action?: string;
}

export interface DashboardVm {
  machineId: string;
  toolId: string;

  telemetryTs?: string;
  predictionTs?: string;

  spindleRpm: number;
  spindleCurrentA: number;
  spindlePowerKw: number;
  feedMmMin: number;
  program: string;
  cutFlag: boolean;

  machineState: string;
  spindleState: string;
  stopRequired: boolean;
  stopReason?: string;
  controlAction?: string;

  rulMinutes?: number;
  alarmLevel: number;
  alarmCode: string;
  state: string;
  message: string;
  requiredAction: string;
  modelVersion?: string;

  events: MachineEventView[];
}

export interface MachineEventView {
  id?: number;
  ts?: string;
  eventCode: string;
  eventLevel: number;
  eventMessage: string;
  machineState?: string;
  spindleState?: string;
  stopReason?: string;
  controlAction?: string;
}