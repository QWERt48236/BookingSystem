import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAlertTriangle, LucideArrowLeft, LucideCalendarPlus, LucideCheckCircle2 } from '@lucide/angular';
import { Subscription } from 'rxjs';
import { BookingService } from '../../core/services/booking';
import { ResourceService } from '../../core/services/resource';
import { ResourceHubService } from '../../core/services/resource-hub';
import { ResourceDetailResponse, SlotBookedEvent, SlotResponse } from '../../core/models/resource.model';
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
export class Detail implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly resourceService = inject(ResourceService);
  private readonly bookingService = inject(BookingService);
  private readonly resourceHub = inject(ResourceHubService);

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

  private resourceId!: number;
  private readonly subscription = new Subscription();

  constructor() {
    this.subscription.add(this.route.paramMap.subscribe((params) => this.onRouteParamsChanged(Number(params.get('id')))));
    this.subscription.add(this.resourceHub.slotBooked.subscribe((event) => this.onSlotBooked(event)));
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    void this.resourceHub.leaveCurrentGroup();
  }

  private onRouteParamsChanged(newResourceId: number): void {
    if (this.resourceId === newResourceId) {
      return;
    }

    void this.resourceHub.leaveCurrentGroup();
    this.resourceId = newResourceId;
    this.bookingConfirmed.set(false);
    this.bookingError.set(false);
    this.selectedSlotId = null;
    this.loadResource();
    void this.resourceHub.joinResourceGroup(this.resourceId);
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
    this.loadResource();
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

  private loadResource(): void {
    this.loading.set(true);
    this.resourceService.getById(this.resourceId, this.selectedDate).subscribe({
      next: (resource) => {
        this.resource.set(resource);
        this.selectedSlotId = resource.slots.find((s) => !s.isBooked)?.id ?? resource.slots[0]?.id ?? null;
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private onSlotBooked(event: SlotBookedEvent): void {
    if (event.resourceId !== this.resourceId || event.date !== this.selectedDate) {
      return;
    }

    const current = this.resource();
    if (!current) {
      return;
    }

    this.resource.set({
      ...current,
      slots: current.slots.map((s) => (s.id === event.slotId ? { ...s, isBooked: true } : s)),
    });

    if (this.selectedSlotId === event.slotId) {
      this.selectedSlotId = current.slots.find((s) => s.id !== event.slotId && !s.isBooked)?.id ?? null;
    }
  }
}
