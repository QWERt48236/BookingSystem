import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideAlertTriangle } from '@lucide/angular';
import { AuthService } from '../../core/services/auth';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, LucideAlertTriangle],
  templateUrl: './login.html',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected email = '';
  protected password = '';
  protected readonly loginError = signal(false);

  protected submit(): void {
    if (!this.email || !this.password) {
      this.loginError.set(true);
      return;
    }

    this.auth.login({ email: this.email, password: this.password }).subscribe({
      next: () => {
        this.loginError.set(false);
        this.router.navigate(['/']);
      },
      error: () => this.loginError.set(true),
    });
  }
}
