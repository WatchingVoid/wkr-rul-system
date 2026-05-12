import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { merge, shareReplay, Subject, switchMap, timer } from 'rxjs';
import { DashboardApiService } from './services/dashboard-api.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  machineId = 'HAAS_VF2_NGC_01';
  toolId = 'T12';

  private readonly manualRefresh$ = new Subject<void>();

  readonly vm$ = merge(timer(0, 2000), this.manualRefresh$).pipe(
    switchMap(() => this.api.getDashboard(this.machineId, this.toolId)),
    shareReplay({ bufferSize: 1, refCount: true })
  );

  constructor(private readonly api: DashboardApiService) {}

  refresh(): void {
    this.manualRefresh$.next();
  }

  stateClass(state?: string): string {
    switch ((state ?? '').toLowerCase()) {
      case 'critical':
        return 'state-critical';
      case 'warning':
        return 'state-warning';
      case 'normal':
        return 'state-normal';
      default:
        return 'state-unknown';
    }
  }

  processClass(state?: string): string {
    switch ((state ?? '').toLowerCase()) {
      case 'cutting':
        return 'process-cutting';
      case 'running':
        return 'process-running';
      case 'stopped':
        return 'process-stopped';
      case 'idle':
        return 'process-idle';
      default:
        return 'process-unknown';
    }
  }

  eventClass(level: number): string {
    if (level >= 2) return 'event-critical';
    if (level === 1) return 'event-warning';
    return 'event-normal';
  }

  formatNumber(value: number | null | undefined, digits = 1): string {
    if (value === undefined || value === null || Number.isNaN(value)) {
      return '-';
    }

    return value.toLocaleString('ru-RU', {
      minimumFractionDigits: digits,
      maximumFractionDigits: digits
    });
  }

  formatDate(value?: string | null): string {
    if (!value) return '-';

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;

    return date.toLocaleString('ru-RU');
  }
}