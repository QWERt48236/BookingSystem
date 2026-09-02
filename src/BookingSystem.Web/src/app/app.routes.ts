import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { Login } from './features/auth/login';
import { Register } from './features/auth/register';
import { Browse } from './features/resources/browse';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: '', component: Browse, canActivate: [authGuard] },
  { path: '**', redirectTo: '' },
];
