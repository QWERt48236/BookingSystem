import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAlertTriangle, LucideArrowLeft, LucideCalendarPlus, LucideCheckCircle2 } from '@lucide/angular';
import { BookingService } from '../../core/services/booking';
import { ResourceService } from '../../core/services/resource';
import { ResourceDetailResponse, SlotResponse } from '../../core/models/resource.model';
import { extractErrorMessage } from '../../core/utils/http-error';

function todayIso(): string {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

function timeLabel(start: string, end: string): string {
  return `${start.slice(0, 5)}–${end.slice(0, 5)}`;
}

@Component({
  selector: 'app-resource-detail',
  imports: [FormsModule, RouterLink, LucideAlertTriangle, LucideArrowLeft, LucideCalendarPlus, LucideCheckCircle2],
  templateUrl: './detail.html',
})
export class Detail {
  private readonly route = inject(ActivatedRoute);
  private readonly resourceService = inject(ResourceService);
  private readonly bookingService = inject(BookingService);

  protected readonly resource = signal<ResourceDetailResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly today = todayIso();

  protected selectedDate = todayIso();
  protected selectedSlotId: number | null = null;

  protected readonly booking = signal(false);
  protected readonly bookingConfirmed = signal(false);
  protected readonly confirmedSlotLabel = signal('');
  protected readonly bookingError = signal(false);
  protected readonly bookingErrorText = signal('');

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.resourceService.getById(id).subscribe({
      next: (resource) => {
        this.resource.set(resource);
        this.selectedSlotId = resource.slots[0]?.id ?? null;
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected slotLabel(slot: SlotResponse): string {
    return timeLabel(slot.startTime, slot.endTime);
  }

  protected selectSlot(slotId: number): void {
    this.selectedSlotId = slotId;
    this.bookingError.set(false);
  }

  protected onDateChange(): void {
    this.bookingError.set(false);
    this.bookingConfirmed.set(false);
  }

  protected submitBooking(): void {
    const resource = this.resource();
    if (!resource || this.selectedSlotId === null) {
      return;
    }

    const slot = resource.slots.find((s) => s.id === this.selectedSlotId);
    this.booking.set(true);
    this.bookingService.create({ slotId: this.selectedSlotId, date: this.selectedDate }).subscribe({
      next: () => {
        this.booking.set(false);
        this.bookingConfirmed.set(true);
        this.bookingError.set(false);
        this.confirmedSlotLabel.set(slot ? this.slotLabel(slot) : '');
      },
      error: (err: HttpErrorResponse) => {
        this.booking.set(false);
        this.bookingError.set(true);
        this.bookingErrorText.set(extractErrorMessage(err, 'Something went wrong — please try again.'));
      },
    });
  }
}
