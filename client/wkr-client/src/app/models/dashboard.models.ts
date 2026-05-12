export interface DashboardCurrentDto {
  machineId: string;
  toolId: string;
  lastTelemetry?: DashboardTelemetryPointDto | null;
  lastPrediction?: DashboardRulPointDto | null;
  lastAlarm?: DashboardAlarmDto | null;
  machineEvents: DashboardMachineEventDto[];
}

export interface DashboardTelemetryPointDto {
  id: number;
  ts: string;

  machineId: string;
  toolId: string;

  spindleRpm: number;
  spindleCurrentA: number;
  spindlePowerKw: number;
  feedMmMin: number;

  program?: string | null;
  cutFlag: boolean;

  toolDiameterMm?: number | null;

  cuttingSpeedMmin?: number | null;
  powerFromTorqueKw?: number | null;
  tangentialForceN?: number | null;

  machineState: string;
  spindleState: string;
  stopRequired: boolean;
  stopReason?: string | null;
  controlAction?: string | null;

  isCutting: boolean;
  isStopped: boolean;
}

export interface DashboardRulPointDto {
  id: number;
  ts: string;

  machineId: string;
  toolId: string;

  rulMinutes: number;
  alarmLevel: number;
  alarmCode: string;
  state: string;
  message: string;
  requiredAction: string;
  modelVersion: string;
}

export interface DashboardAlarmDto {
  id: number;
  ts: string;

  machineId: string;
  toolId: string;

  rulMinutes: number;
  alarmLevel: number;
  alarmCode: string;
  alarmMessage: string;
}

export interface DashboardMachineEventDto {
  id: number;
  ts: string;

  machineId: string;
  toolId?: string | null;

  eventCode: string;
  eventLevel: number;
  eventMessage: string;

  machineState?: string | null;
  spindleState?: string | null;
  stopReason?: string | null;
  controlAction?: string | null;
}

export interface DashboardVm {
  machineId: string;
  toolId: string;

  current?: DashboardCurrentDto | null;
  telemetryHistory: DashboardTelemetryPointDto[];
  rulHistory: DashboardRulPointDto[];

  lastTelemetry?: DashboardTelemetryPointDto | null;
  lastPrediction?: DashboardRulPointDto | null;
  lastAlarm?: DashboardAlarmDto | null;
  machineEvents: DashboardMachineEventDto[];

  processState: string;
  processText: string;

  powerPath: string;
  currentPath: string;
  rpmPath: string;
  cuttingSpeedPath: string;
  forcePath: string;
  rulPath: string;
}