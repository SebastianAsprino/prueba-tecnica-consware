import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL } from './api-config';

const STORAGE_KEY = 'admin-token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${API_BASE_URL}/auth`;

  private readonly token = signal<string | null>(this.readStoredToken());
  readonly isLoggedIn = computed(() => this.token() !== null);

  getToken(): string | null {
    return this.token();
  }

  login(username: string, password: string): Observable<{ token: string }> {
    return this.http.post<{ token: string }>(`${this.baseUrl}/login`, { username, password }).pipe(
      tap((response) => {
        this.token.set(response.token);
        this.writeStoredToken(response.token);
      }),
    );
  }

  logout(): void {
    this.token.set(null);
    this.writeStoredToken(null);
  }

  private readStoredToken(): string | null {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch {
      return null;
    }
  }

  private writeStoredToken(token: string | null): void {
    try {
      if (token) {
        localStorage.setItem(STORAGE_KEY, token);
      } else {
        localStorage.removeItem(STORAGE_KEY);
      }
    } catch {
      // localStorage no disponible (modo privado, etc.): la sesión no persiste entre recargas.
    }
  }
}
