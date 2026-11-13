import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { API_UNREACHABLE_MESSAGE } from '../../core/http-error';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly authService = inject(AuthService);

  username = '';
  password = '';

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  submit(): void {
    if (!this.username.trim() || !this.password.trim()) {
      this.errorMessage.set('Usuario y contraseña son obligatorios.');
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.username.trim(), this.password).subscribe({
      next: () => this.loading.set(false),
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.errorMessage.set(
          err.status === 401 ? 'Usuario o contraseña incorrectos.' : API_UNREACHABLE_MESSAGE,
        );
      },
    });
  }
}
