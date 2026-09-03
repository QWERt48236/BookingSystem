import { HttpClient } from '@angular/common/http';
import { Injectable, Injector, computed, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.model';
import { ResourceHubService } from './resource-hub';

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

function rolesFromPayload(payload: JwtPayload | null): string[] {
  const raw = payload?.[ROLE_CLAIM] ?? payload?.role;
  if (!raw) {
    return [];
  }
  return Array.isArray(raw) ? raw : [raw];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenSignal = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  // Resolved lazily (only inside logout, never at construction) so this
  // doesn't form a constructor-time circular dependency with ResourceHubService,
  // which injects AuthService itself.
  private readonly injector = inject(Injector);

  readonly isAuthenticated = computed(() => this.tokenSignal() !== null);
  readonly roles = computed(() => rolesFromPayload(this.currentPayload()));
  readonly isAdmin = computed(() => this.roles().includes('Admin'));

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
    void this.injector.get(ResourceHubService).disconnect();
  }
}
