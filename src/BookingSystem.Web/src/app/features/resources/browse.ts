import { Component, inject, signal } from '@angular/core';
import { ResourceService } from '../../core/services/resource';
import { ResourceResponse } from '../../core/models/resource.model';

@Component({
  selector: 'app-browse',
  imports: [],
  templateUrl: './browse.html',
})
export class Browse {
  private readonly resourceService = inject(ResourceService);

  protected readonly resources = signal<ResourceResponse[]>([]);
  protected readonly loading = signal(true);

  constructor() {
    this.resourceService.getAll().subscribe({
      next: (resources) => {
        this.resources.set(resources);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
