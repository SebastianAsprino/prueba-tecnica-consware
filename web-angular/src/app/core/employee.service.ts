import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { Employee, ImportEmployeesResult } from './employee.model';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/employees`;

  list(): Observable<Employee[]> {
    return this.http.get<Employee[]>(this.baseUrl);
  }

  create(name: string): Observable<Employee> {
    return this.http.post<Employee>(this.baseUrl, { name });
  }

  importCsv(file: File): Observable<ImportEmployeesResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ImportEmployeesResult>(`${this.baseUrl}/import`, formData);
  }
}
