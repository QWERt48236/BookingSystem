import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideAlertTriangle, LucidePencil, LucidePlus, LucideTrash2 } from '@lucide/angular';
import { ResourceService } from '../../core/services/resource';
import { ResourceDetailResponse, ResourceResponse } from '../../core/models/resource.model';
import { extractErrorMessage } from '../../core/utils/http-error';

function timeLabel(start: string, end: string): string {
  return `${start.slice(0, 5)}–${end.slice(0, 5)}`;
}

@Component({
  selector: 'app-admin-resources',
  imports: [FormsModule, LucideAlertTriangle, LucidePencil, LucidePlus, LucideTrash2],
  templateUrl: './resources.html',
})
export class AdminResources {
  private readonly resourceService = inject(ResourceService);

  protected readonly resources = signal<ResourceResponse[]>([]);
  protected readonly loading = signal(true);
  protected readonly listError = signal('');

  protected readonly addDialogOpen = signal(false);
  protected newResourceName = '';
  protected readonly addError = signal('');

  protected readonly editDialogOpen = signal(false);
  protected readonly editTarget = signal<ResourceDetailResponse | null>(null);
  protected editName = '';
  protected readonly editError = signal('');
  protected newSlotStart = '';
  protected newSlotEnd = '';
  protected readonly slotError = signal('');

  constructor() {
    this.loadResources();
  }

  private loadResources(): void {
    this.loading.set(true);
    this.resourceService.getAll().subscribe({
      next: (resources) => {
        this.resources.set(resources);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected openAddDialog(): void {
    this.newResourceName = '';
    this.addError.set('');
    this.addDialogOpen.set(true);
  }

  protected closeAddDialog(): void {
    this.addDialogOpen.set(false);
  }

  protected confirmAddResource(): void {
    if (!this.newResourceName.trim()) {
      return;
    }

    this.resourceService.create({ name: this.newResourceName.trim() }).subscribe({
      next: () => {
        this.addDialogOpen.set(false);
        this.loadResources();
      },
      error: () => this.addError.set('Could not create resource — please try again.'),
    });
  }

  protected openEditDialog(resource: ResourceResponse): void {
    this.editError.set('');
    this.slotError.set('');
    this.newSlotStart = '';
    this.newSlotEnd = '';
    this.editDialogOpen.set(true);
    this.editTarget.set(null);
    this.resourceService.getById(resource.id).subscribe({
      next: (detail) => {
        this.editTarget.set(detail);
        this.editName = detail.name;
      },
      error: () => {
        this.editError.set('Could not load resource.');
      },
    });
  }

  protected closeEditDialog(): void {
    this.editDialogOpen.set(false);
    this.editTarget.set(null);
  }

  protected saveName(): void {
    const target = this.editTarget();
    if (!target || !this.editName.trim()) {
      return;
    }

    this.resourceService.update(target.id, { name: this.editName.trim() }).subscribe({
      next: () => {
        this.editTarget.set({ ...target, name: this.editName.trim() });
        this.loadResources();
      },
      error: () => this.editError.set('Could not save changes.'),
    });
  }

  protected addSlot(): void {
    const target = this.editTarget();
    if (!target || !this.newSlotStart || !this.newSlotEnd) {
      return;
    }

    this.slotError.set('');
    this.resourceService.addSlots(target.id, [{ startTime: this.newSlotStart, endTime: this.newSlotEnd }]).subscribe({
      next: (slots) => {
        this.editTarget.set({ ...target, slots: [...target.slots, ...slots] });
        this.newSlotStart = '';
        this.newSlotEnd = '';
      },
      error: (err: HttpErrorResponse) =>
        this.slotError.set(extractErrorMessage(err, 'Could not add slot — check the times and try again.')),
    });
  }

  protected slotLabel(startTime: string, endTime: string): string {
    return timeLabel(startTime, endTime);
  }

  protected deleteResource(resource: ResourceResponse): void {
    if (!confirm(`Delete "${resource.name}"? This cannot be undone.`)) {
      return;
    }

    this.listError.set('');
    this.resourceService.delete(resource.id).subscribe({
      next: () => this.loadResources(),
      error: (err: HttpErrorResponse) => {
        this.listError.set(extractErrorMessage(err, 'Could not delete resource — please try again.'));
      },
    });
  }
}
