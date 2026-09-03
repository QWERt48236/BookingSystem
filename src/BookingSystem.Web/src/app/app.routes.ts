import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { adminGuard } from './core/guards/admin-guard';
import { Login } from './features/auth/login';
import { Register } from './features/auth/register';
import { Browse } from './features/resources/browse';
import { Detail } from './features/resources/detail';
import { Mine } from './features/bookings/mine';
import { AdminResources } from './features/admin/resources';
import { AdminBookings } from './features/admin/bookings';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: '', component: Browse, canActivate: [authGuard] },
  { path: 'resources/:id', component: Detail, canActivate: [authGuard] },
  { path: 'bookings/mine', component: Mine, canActivate: [authGuard] },
  { path: 'admin', component: AdminResources, canActivate: [authGuard, adminGuard] },
  { path: 'admin/bookings', component: AdminBookings, canActivate: [authGuard, adminGuard] },
  { path: '**', redirectTo: '' },
];
