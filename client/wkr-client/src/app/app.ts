import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BehaviorSubject, switchMap, timer } from 'rxjs';
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

  private readonly refresh$ = new BehaviorSubject<void>(undefined);

  readonly vm$ = timer(0, 2000).pipe(
    switchMap(() => this.refresh$),
    switchMap(() => this.api.getDashboard(this.machineId, this.toolId))
  );

  constructor(private readonly api: DashboardApiService) {}

  refresh(): void {
    this.refresh$.next();
  }

  stateClass(state: string): string {
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

  machineClass(machineState: string): string {
    switch ((machineState ?? '').toLowerCase()) {
      case 'stopped':
        return 'machine-stopped';
      case 'cutting':
        return 'machine-cutting';
      case 'running':
        return 'machine-running';
      case 'idle':
        return 'machine-idle';
      default:
        return 'machine-unknown';
    }
  }

  formatNumber(value: number | undefined, digits = 1): string {
    if (value === undefined || value === null || Number.isNaN(value)) {
      return '-';
    }

    return value.toLocaleString('ru-RU', {
      minimumFractionDigits: digits,
      maximumFractionDigits: digits
    });
  }

  formatDate(value?: string): string {
    if (!value) return '-';

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;

    return date.toLocaleString('ru-RU');
  }
}