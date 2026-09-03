import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { BookingService } from '../../core/services/booking';
import { ResourceService } from '../../core/services/resource';
import { BookingResponse } from '../../core/models/booking.model';
import { ResourceDetailResponse } from '../../core/models/resource.model';

function timeLabel(start: string, end: string): string {
  return `${start.slice(0, 5)}–${end.slice(0, 5)}`;
}

interface BookingRow {
  id: number;
  resourceName: string;
  date: string;
  timeLabel: string;
  createdAt: string;
}

@Component({
  selector: 'app-my-bookings',
  imports: [RouterLink],
  templateUrl: './mine.html',
})
export class Mine {
  private readonly bookingService = inject(BookingService);
  private readonly resourceService = inject(ResourceService);

  protected readonly rows = signal<BookingRow[]>([]);
  protected readonly loading = signal(true);

  constructor() {
    this.bookingService.getMine().subscribe({
      next: (bookings) => this.resolveResourceNames(bookings),
      error: () => this.loading.set(false),
    });
  }

  private resolveResourceNames(bookings: BookingResponse[]): void {
    if (bookings.length === 0) {
      this.rows.set([]);
      this.loading.set(false);
      return;
    }

    this.resourceService.getAll().subscribe({
      next: (resources) => {
        const detailFetches = resources.map((r) => this.resourceService.getById(r.id));
        forkJoin(detailFetches).subscribe({
          next: (details) => {
            const slotIndex = new Map<number, { resourceName: string; timeLabel: string }>();
            for (const detail of details as ResourceDetailResponse[]) {
              for (const slot of detail.slots) {
                slotIndex.set(slot.id, {
                  resourceName: detail.name,
                  timeLabel: timeLabel(slot.startTime, slot.endTime),
                });
              }
            }

            this.rows.set(
              bookings.map((b) => {
                const info = slotIndex.get(b.slotId);
                return {
                  id: b.id,
                  resourceName: info?.resourceName ?? `Slot #${b.slotId}`,
                  date: b.date,
                  timeLabel: info?.timeLabel ?? '—',
                  createdAt: b.createdAt.slice(0, 10),
                };
              }),
            );
            this.loading.set(false);
          },
          error: () => this.loading.set(false),
        });
      },
      error: () => this.loading.set(false),
    });
  }
}
