import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { ExpenseRequest, ExpenseStatus } from './expense-request.model';

@Injectable({ providedIn: 'root' })
export class ExpenseRequestService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/expense-requests`;

  list(status?: ExpenseStatus | ''): Observable<ExpenseRequest[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<ExpenseRequest[]>(this.baseUrl, { params });
  }

  approve(id: number): Observable<ExpenseRequest> {
    return this.http.post<ExpenseRequest>(`${this.baseUrl}/${id}/approve`, {});
  }

  reject(id: number, reason: string): Observable<ExpenseRequest> {
    return this.http.post<ExpenseRequest>(`${this.baseUrl}/${id}/reject`, { reason });
  }
}
