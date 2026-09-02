import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.model';

const TOKEN_KEY = 'bookingSystem.token';
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

interface JwtPayload {
  [ROLE_CLAIM]?: string | string[];
  role?: string | string[];
  exp?: number;
}

function decodeJwt(token: string): JwtPayload | null {
  const parts = token.split('.');
  if (parts.length !== 3) {
    return null;
  }

  try {
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    return JSON.parse(atob(padded));
  } catch {
    return null;
  }
}

function roleFromPayload(payload: JwtPayload | null): string | null {
  const raw = payload?.[ROLE_CLAIM] ?? payload?.role;
  if (!raw) {
    return null;
  }
  return Array.isArray(raw) ? raw[0] : raw;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenSignal = signal<string | null>(localStorage.getItem(TOKEN_KEY));

  readonly isAuthenticated = computed(() => this.tokenSignal() !== null);
  readonly role = computed(() => roleFromPayload(this.currentPayload()));

  private readonly currentPayload = computed(() => {
    const token = this.tokenSignal();
    return token ? decodeJwt(token) : null;
  });

  constructor(private readonly http: HttpClient) {}

  token(): string | null {
    return this.tokenSignal();
  }

  login(request: LoginRequest) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap((response) => {
        localStorage.setItem(TOKEN_KEY, response.token);
        this.tokenSignal.set(response.token);
      }),
    );
  }

  register(request: RegisterRequest) {
    return this.http.post<void>(`${environment.apiUrl}/auth/register`, request);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.tokenSignal.set(null);
  }
}
