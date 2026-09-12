import { Component, inject, signal } from '@angular/core';
import { AuthService } from './core/auth.service';
import { Login } from './features/login/login';
import { RequestsTable } from './features/requests-table/requests-table';
import { Employees } from './features/employees/employees';
import { Historial } from './features/historial/historial';

type Tab = 'requests' | 'employees' | 'historial';

@Component({
  imports: [Login, RequestsTable, Employees, Historial],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly authService = inject(AuthService);
  protected readonly activeTab = signal<Tab>('requests');

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
  }

  logout(): void {
    this.authService.logout();
  }
}
