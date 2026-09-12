import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EmployeeService } from '../../core/employee.service';
import { Employee, ImportEmployeesResult } from '../../core/employee.model';

@Component({
  selector: 'app-employees',
  imports: [FormsModule],
  templateUrl: './employees.html',
  styleUrl: './employees.css',
})
export class Employees {
  private readonly employeeService = inject(EmployeeService);
  private readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');

  readonly employees = signal<Employee[]>([]);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly importResult = signal<ImportEmployeesResult | null>(null);

  newEmployeeName = '';

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.employeeService.list().subscribe({
      next: (employees) => {
        this.employees.set(employees);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.describeError(err));
        this.loading.set(false);
      },
    });
  }

  createEmployee(): void {
    if (!this.newEmployeeName.trim()) {
      this.errorMessage.set('El nombre del empleado es obligatorio.');
      return;
    }

    this.errorMessage.set(null);
    this.employeeService.create(this.newEmployeeName.trim()).subscribe({
      next: () => {
        this.newEmployeeName = '';
        this.load();
      },
      error: (err: HttpErrorResponse) => this.errorMessage.set(this.describeError(err)),
    });
  }

  onCsvSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    this.errorMessage.set(null);
    this.importResult.set(null);

    this.employeeService.importCsv(file).subscribe({
      next: (result) => {
        this.importResult.set(result);
        this.fileInput()!.nativeElement.value = '';
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(this.describeError(err));
        this.fileInput()!.nativeElement.value = '';
      },
    });
  }

  private describeError(err: HttpErrorResponse): string {
    if (err.status === 0) {
      return 'No se pudo conectar con la API. Verifica que esté corriendo en http://localhost:5098.';
    }
    const apiErrors = err.error?.errors as string[] | undefined;
    if (apiErrors?.length) {
      return apiErrors.join(' ');
    }
    return `Ocurrió un error inesperado (código ${err.status}).`;
  }
}
