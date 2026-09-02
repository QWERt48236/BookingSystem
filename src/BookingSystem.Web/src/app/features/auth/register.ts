import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideAlertTriangle } from '@lucide/angular';
import { AuthService } from '../../core/services/auth';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink, LucideAlertTriangle],
  templateUrl: './register.html',
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected email = '';
  protected password = '';
  protected confirmPassword = '';
  protected readonly regError = signal(false);
  protected readonly regErrorText = signal('');

  protected submit(): void {
    if (!this.email || !this.password) {
      this.setError('Email and password are required.');
      return;
    }
    if (this.password !== this.confirmPassword) {
      this.setError('Passwords do not match.');
      return;
    }

    this.auth.register({ email: this.email, password: this.password }).subscribe({
      next: () => {
        this.regError.set(false);
        this.router.navigate(['/login']);
      },
      error: (err: HttpErrorResponse) => {
        const messages: string[] = Array.isArray(err.error) ? err.error : [];
        this.setError(messages.join(' ') || 'Registration failed.');
      },
    });
  }

  private setError(text: string): void {
    this.regError.set(true);
    this.regErrorText.set(text);
  }
}
