import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { BookingService } from '../../core/services/booking';
import { AdminBookingResponse } from '../../core/models/booking.model';
import { extractErrorMessage } from '../../core/utils/http-error';

const PAGE_SIZE = 50;

function timeLabel(start: string, end: string): string {
  return `${start.slice(0, 5)}–${end.slice(0, 5)}`;
}

interface BookingRow {
  id: number;
  resourceName: string;
  date: string;
  timeLabel: string;
  userLabel: string;
  createdAt: string;
}

@Component({
  selector: 'app-admin-bookings',
  imports: [],
  templateUrl: './bookings.html',
})
export class AdminBookings {
  private readonly bookingService = inject(BookingService);

  protected readonly rows = signal<BookingRow[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal('');
  protected readonly page = signal(1);
  protected readonly hasNextPage = signal(false);

  constructor() {
    this.loadPage(1);
  }

  protected nextPage(): void {
    if (this.hasNextPage()) {
      this.loadPage(this.page() + 1);
    }
  }

  protected previousPage(): void {
    if (this.page() > 1) {
      this.loadPage(this.page() - 1);
    }
  }

  private loadPage(page: number): void {
    this.loading.set(true);
    this.loadError.set('');

    this.bookingService.getAll(page, PAGE_SIZE).subscribe({
      next: (bookings) => {
        this.page.set(page);
        this.hasNextPage.set(bookings.length === PAGE_SIZE);
        this.rows.set(bookings.map(toRow));
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loadError.set(extractErrorMessage(err, 'Could not load bookings — please try again.'));
        this.loading.set(false);
      },
    });
  }
}

function toRow(b: AdminBookingResponse): BookingRow {
  return {
    id: b.id,
    resourceName: b.resourceName,
    date: b.date,
    timeLabel: timeLabel(b.slotStartTime, b.slotEndTime),
    userLabel: b.userEmail ?? b.userId,
    createdAt: b.createdAt.slice(0, 10),
  };
}
