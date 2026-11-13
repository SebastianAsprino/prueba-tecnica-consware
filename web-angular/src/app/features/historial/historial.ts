import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { describeHttpError } from '../../core/http-error';
import { ExpenseRequestService } from '../../core/expense-request.service';
import { ExpenseRequest } from '../../core/expense-request.model';

type Movimiento = 'AutoAprobado' | 'Aprobado' | 'Rechazado' | 'Pendiente';

@Component({
  selector: 'app-historial',
  imports: [DecimalPipe, DatePipe, FormsModule],
  templateUrl: './historial.html',
  styleUrl: './historial.css',
})
export class Historial {
  private readonly expenseRequestService = inject(ExpenseRequestService);

  readonly requests = signal<ExpenseRequest[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly employeeFilter = signal('');
  readonly movementFilter = signal<Movimiento | ''>('');
  readonly monthFilter = signal(this.currentMonth());

  readonly filteredRequests = computed(() => {
    const employeeTerm = this.employeeFilter().trim().toLowerCase();
    const movement = this.movementFilter();
    return this.requests().filter((request) => {
      const matchesEmployee = !employeeTerm || request.employeeName.toLowerCase().includes(employeeTerm);
      const matchesMovement = !movement || this.movementOf(request) === movement;
      return matchesEmployee && matchesMovement;
    });
  });

  readonly monthlyRequests = computed(() => {
    const month = this.monthFilter();
    if (!month) {
      return this.filteredRequests();
    }
    return this.filteredRequests().filter((request) => request.createdAt.slice(0, 7) === month);
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.expenseRequestService.list('').subscribe({
      next: (requests) => {
        this.requests.set(requests);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(describeHttpError(err));
        this.loading.set(false);
      },
    });
  }

  movementOf(request: ExpenseRequest): Movimiento {
    if (request.status === 'Pending') {
      return 'Pendiente';
    }
    if (request.status === 'Rejected') {
      return 'Rechazado';
    }
    return request.autoApproved ? 'AutoAprobado' : 'Aprobado';
  }

  movementLabel(movement: Movimiento): string {
    switch (movement) {
      case 'AutoAprobado':
        return 'Auto-aprobado';
      case 'Aprobado':
        return 'Aprobado';
      case 'Rechazado':
        return 'Rechazado';
      default:
        return 'Pendiente';
    }
  }

  movementBadgeClass(movement: Movimiento): string {
    switch (movement) {
      case 'AutoAprobado':
        return 'badge-info';
      case 'Aprobado':
        return 'badge-success';
      case 'Rechazado':
        return 'badge-error';
      default:
        return 'badge-warning';
    }
  }

  downloadCsv(): void {
    const rows = this.monthlyRequests();
    const header = [
      'Empleado',
      'Monto',
      'Categoría',
      'Descripción',
      'Movimiento',
      'Fecha creación',
      'Fecha decisión',
      'Motivo rechazo',
    ];
    const lines = rows.map((request) => [
      request.employeeName,
      request.amount,
      request.category,
      request.description,
      this.movementLabel(this.movementOf(request)),
      request.createdAt,
      request.decidedAt ?? '',
      request.rejectionReason ?? '',
    ]);
    const csv = [header, ...lines].map((row) => row.map(this.escapeCsvValue).join(',')).join('\r\n');

    const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    const monthSuffix = this.monthFilter() || 'todos';
    link.href = url;
    link.download = `historial-${monthSuffix}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }

  private currentMonth(): string {
    return new Date().toISOString().slice(0, 7);
  }

  private escapeCsvValue(value: string | number): string {
    const text = String(value);
    return /[",\r\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
  }
}
