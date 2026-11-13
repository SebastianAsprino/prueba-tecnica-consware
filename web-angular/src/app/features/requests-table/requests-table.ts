import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { describeHttpError } from '../../core/http-error';
import { ExpenseRequestService } from '../../core/expense-request.service';
import { ExpenseRequest, ExpenseStatus } from '../../core/expense-request.model';

type StatusFilter = ExpenseStatus | '';

@Component({
  selector: 'app-requests-table',
  imports: [DecimalPipe, FormsModule],
  templateUrl: './requests-table.html',
  styleUrl: './requests-table.css',
})
export class RequestsTable {
  private readonly expenseRequestService = inject(ExpenseRequestService);

  readonly statusFilter = signal<StatusFilter>('Pending');
  readonly requests = signal<ExpenseRequest[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly actionErrorMessage = signal<string | null>(null);

  readonly rejectingRequest = signal<ExpenseRequest | null>(null);
  readonly rejectReason = signal('');
  readonly rejectReasonError = signal<string | null>(null);

  constructor() {
    this.load();
  }

  onFilterChange(value: string): void {
    this.statusFilter.set(value as StatusFilter);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.expenseRequestService.list(this.statusFilter()).subscribe({
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

  approve(request: ExpenseRequest): void {
    this.actionErrorMessage.set(null);
    this.expenseRequestService.approve(request.id).subscribe({
      next: () => this.load(),
      error: (err: HttpErrorResponse) => this.actionErrorMessage.set(describeHttpError(err)),
    });
  }

  openRejectModal(request: ExpenseRequest): void {
    this.rejectingRequest.set(request);
    this.rejectReason.set('');
    this.rejectReasonError.set(null);
  }

  closeRejectModal(): void {
    this.rejectingRequest.set(null);
  }

  confirmReject(): void {
    const request = this.rejectingRequest();
    if (!request) {
      return;
    }
    const reason = this.rejectReason().trim();
    if (!reason) {
      this.rejectReasonError.set('El motivo de rechazo es obligatorio.');
      return;
    }

    this.actionErrorMessage.set(null);
    this.expenseRequestService.reject(request.id, reason).subscribe({
      next: () => {
        this.closeRejectModal();
        this.load();
      },
      error: (err: HttpErrorResponse) => this.actionErrorMessage.set(describeHttpError(err)),
    });
  }

  statusBadgeClass(status: ExpenseStatus): string {
    switch (status) {
      case 'Approved':
        return 'badge-success';
      case 'Rejected':
        return 'badge-error';
      default:
        return 'badge-warning';
    }
  }
}
