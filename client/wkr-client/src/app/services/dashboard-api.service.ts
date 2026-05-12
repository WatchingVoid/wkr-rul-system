import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, forkJoin, map, Observable, of } from 'rxjs';
import {
  DashboardCurrentDto,
  DashboardRulPointDto,
  DashboardTelemetryPointDto,
  DashboardVm
} from '../models/dashboard.models';

@Injectable({
  providedIn: 'root'
})
export class DashboardApiService {
  constructor(private readonly http: HttpClient) {}

  getDashboard(machineId: string, toolId: string): Observable<DashboardVm> {
    const baseParams = new HttpParams()
      .set('machineId', machineId)
      .set('toolId', toolId);

    const currentParams = baseParams.set('eventLimit', 20);
    const historyParams = baseParams.set('limit', 120);

    return forkJoin({
      current: this.http
        .get<DashboardCurrentDto>('/api/dashboard/current', { params: currentParams })
        .pipe(catchError(() => of(null))),

      telemetryHistory: this.http
        .get<DashboardTelemetryPointDto[]>('/api/dashboard/telemetry-history', { params: historyParams })
        .pipe(catchError(() => of([]))),

      rulHistory: this.http
        .get<DashboardRulPointDto[]>('/api/dashboard/rul-history', { params: historyParams })
        .pipe(catchError(() => of([])))
    }).pipe(
      map(({ current, telemetryHistory, rulHistory }) => {
        const lastTelemetry = current?.lastTelemetry ?? telemetryHistory.at(-1) ?? null;
        const lastPrediction = current?.lastPrediction ?? rulHistory.at(-1) ?? null;

        return {
          machineId,
          toolId,

          current,
          telemetryHistory,
          rulHistory,

          lastTelemetry,
          lastPrediction,
          lastAlarm: current?.lastAlarm ?? null,
          machineEvents: current?.machineEvents ?? [],

          processState: this.getProcessState(lastTelemetry),
          processText: this.getProcessText(lastTelemetry),

          powerPath: this.buildPath(telemetryHistory, x => x.spindlePowerKw),
          currentPath: this.buildPath(telemetryHistory, x => x.spindleCurrentA),
          rpmPath: this.buildPath(telemetryHistory, x => x.spindleRpm),
          cuttingSpeedPath: this.buildPath(telemetryHistory, x => x.cuttingSpeedMmin ?? 0),
          forcePath: this.buildPath(telemetryHistory, x => x.tangentialForceN ?? 0),
          rulPath: this.buildPath(rulHistory, x => x.rulMinutes)
        };
      })
    );
  }

  private getProcessState(t?: DashboardTelemetryPointDto | null): string {
    if (!t) return 'unknown';

    if (t.stopRequired || t.isStopped || t.machineState === 'stopped') {
      return 'stopped';
    }

    if (t.isCutting || t.cutFlag) {
      return 'cutting';
    }

    if (t.spindleRpm > 0) {
      return 'running';
    }

    return 'idle';
  }

  private getProcessText(t?: DashboardTelemetryPointDto | null): string {
    const state = this.getProcessState(t);

    switch (state) {
      case 'cutting':
        return 'Резание активно';
      case 'running':
        return 'Шпиндель вращается без активного резания';
      case 'stopped':
        return 'Станок остановлен';
      case 'idle':
        return 'Станок в ожидании';
      default:
        return 'Нет данных о процессе';
    }
  }

  private buildPath<T>(items: T[], selector: (item: T) => number): string {
    if (!items.length) return '';

    const values = items.map(selector).map(v => Number.isFinite(v) ? v : 0);
    const min = Math.min(...values);
    const max = Math.max(...values);
    const range = max - min || 1;

    const width = 100;
    const height = 36;

    return values
      .map((value, index) => {
        const x = items.length === 1 ? 0 : (index / (items.length - 1)) * width;
        const y = height - ((value - min) / range) * height;
        return `${x.toFixed(2)},${y.toFixed(2)}`;
      })
      .join(' ');
  }
}