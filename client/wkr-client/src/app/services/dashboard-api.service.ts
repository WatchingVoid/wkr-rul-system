import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, forkJoin, map, Observable, of } from 'rxjs';
import {
  DashboardVm,
  MachineEventDto,
  MachineEventView,
  MachineStateDto,
  RulPredictionDto
} from '../models/dashboard.models';

@Injectable({
  providedIn: 'root'
})
export class DashboardApiService {
  constructor(private readonly http: HttpClient) {}

  getDashboard(machineId: string, toolId: string): Observable<DashboardVm> {
    const machineParams = new HttpParams().set('machineId', machineId);

    const rulParams = new HttpParams()
      .set('machineId', machineId)
      .set('toolId', toolId);

    const eventsParams = new HttpParams()
      .set('machineId', machineId)
      .set('limit', 20);

    return forkJoin({
      machine: this.http.get<MachineStateDto>('/api/machine/last', { params: machineParams })
        .pipe(catchError(() => of(null))),

      rul: this.http.get<RulPredictionDto>('/api/rul/last', { params: rulParams })
        .pipe(catchError(() => of(null))),

      events: this.http.get<MachineEventDto[]>('/api/machine/events', { params: eventsParams })
        .pipe(catchError(() => of([])))
    }).pipe(
      map(({ machine, rul, events }) => this.toDashboardVm(machineId, toolId, machine, rul, events))
    );
  }

  private toDashboardVm(
    machineId: string,
    toolId: string,
    machine: MachineStateDto | null,
    rul: RulPredictionDto | null,
    events: MachineEventDto[]
  ): DashboardVm {
    const machineState = this.getString(machine?.machineState, machine?.machine_state, 'unknown');
    const spindleState = this.getString(machine?.spindleState, machine?.spindle_state, 'unknown');

    const alarmLevel = this.getNumber(rul?.alarmLevel, rul?.alarm_level, 0);
    const state = this.getString(rul?.state, undefined, this.stateFromAlarmLevel(alarmLevel));

    return {
      machineId: this.getString(machine?.machineId, machine?.machine_id, machineId),
      toolId: this.getString(machine?.toolId, machine?.tool_id, toolId),

      telemetryTs: machine?.ts,
      predictionTs: rul?.ts,

      spindleRpm: this.getNumber(machine?.spindleRpm, machine?.spindle_rpm, 0),
      spindleCurrentA: this.getNumber(machine?.spindleCurrentA, machine?.spindle_current_a, 0),
      spindlePowerKw: this.getNumber(machine?.spindlePowerKw, machine?.spindle_power_kw, 0),
      feedMmMin: this.getNumber(machine?.feedMmMin, machine?.feed_mm_min, 0),
      program: this.getString(machine?.program, undefined, '-'),
      cutFlag: this.getBool(machine?.cutFlag, machine?.cut_flag, false),

      machineState,
      spindleState,
      stopRequired: this.getBool(machine?.stopRequired, machine?.stop_required, false),
      stopReason: this.getStringOrUndefined(machine?.stopReason, machine?.stop_reason),
      controlAction: this.getStringOrUndefined(machine?.controlAction, machine?.control_action),

      rulMinutes: this.getOptionalNumber(rul?.rulMinutes, rul?.rul_minutes),
      alarmLevel,
      alarmCode: this.getString(rul?.alarmCode, rul?.alarm_code, 'NO_PREDICTION'),
      state,
      message: this.getString(rul?.message, undefined, this.defaultMessage(state)),
      requiredAction: this.getString(rul?.requiredAction, rul?.required_action, this.defaultAction(state)),
      modelVersion: this.getStringOrUndefined(rul?.modelVersion, rul?.model_version),

      events: events.map(e => this.toMachineEventView(e))
    };
  }

  private toMachineEventView(e: MachineEventDto): MachineEventView {
    return {
      id: e.id,
      ts: e.ts,
      eventCode: this.getString(e.eventCode, e.event_code, '-'),
      eventLevel: this.getNumber(e.eventLevel, e.event_level, 0),
      eventMessage: this.getString(e.eventMessage, e.event_message, '-'),
      machineState: this.getStringOrUndefined(e.machineState, e.machine_state),
      spindleState: this.getStringOrUndefined(e.spindleState, e.spindle_state),
      stopReason: this.getStringOrUndefined(e.stopReason, e.stop_reason),
      controlAction: this.getStringOrUndefined(e.controlAction, e.control_action)
    };
  }

  private getString(a: unknown, b: unknown, defaultValue: string): string {
    if (typeof a === 'string' && a.trim()) return a;
    if (typeof b === 'string' && b.trim()) return b;
    return defaultValue;
  }

  private getStringOrUndefined(a: unknown, b: unknown): string | undefined {
    if (typeof a === 'string' && a.trim()) return a;
    if (typeof b === 'string' && b.trim()) return b;
    return undefined;
  }

  private getNumber(a: unknown, b: unknown, defaultValue: number): number {
    if (typeof a === 'number' && Number.isFinite(a)) return a;
    if (typeof b === 'number' && Number.isFinite(b)) return b;

    if (typeof a === 'string') {
      const n = Number(a.replace(',', '.'));
      if (Number.isFinite(n)) return n;
    }

    if (typeof b === 'string') {
      const n = Number(b.replace(',', '.'));
      if (Number.isFinite(n)) return n;
    }

    return defaultValue;
  }

  private getOptionalNumber(a: unknown, b: unknown): number | undefined {
    const n = this.getNumber(a, b, Number.NaN);
    return Number.isFinite(n) ? n : undefined;
  }

  private getBool(a: unknown, b: unknown, defaultValue: boolean): boolean {
    if (typeof a === 'boolean') return a;
    if (typeof b === 'boolean') return b;
    return defaultValue;
  }

  private stateFromAlarmLevel(level: number): string {
    if (level >= 2) return 'critical';
    if (level === 1) return 'warning';
    return 'normal';
  }

  private defaultMessage(state: string): string {
    switch (state) {
      case 'critical':
        return 'Критическое состояние инструмента.';
      case 'warning':
        return 'Остаточный ресурс инструмента снижается.';
      case 'normal':
        return 'Состояние инструмента в допустимом диапазоне.';
      default:
        return 'Нет данных о состоянии инструмента.';
    }
  }

  private defaultAction(state: string): string {
    switch (state) {
      case 'critical':
        return 'Остановить обработку и заменить инструмент.';
      case 'warning':
        return 'Подготовить замену инструмента.';
      case 'normal':
        return 'Продолжить обработку.';
      default:
        return 'Ожидание данных.';
    }
  }
}