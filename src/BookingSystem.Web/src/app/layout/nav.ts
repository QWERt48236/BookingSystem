import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { LucideCalendarCheck2, LucideLogOut } from '@lucide/angular';
import { AuthService } from '../core/services/auth';

@Component({
  selector: 'app-nav',
  imports: [LucideCalendarCheck2, LucideLogOut, RouterLink, RouterLinkActive],
  templateUrl: './nav.html',
})
export class Nav {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly roleLabel = () => (this.auth.role() === 'Admin' ? 'Admin' : 'User');
  protected readonly isAdmin = () => this.auth.role() === 'Admin';

  protected logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
