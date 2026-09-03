import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AdminBookingResponse, BookingRequest, BookingResponse } from '../models/booking.model';

@Injectable({ providedIn: 'root' })
export class BookingService {
  constructor(private readonly http: HttpClient) {}

  create(request: BookingRequest) {
    return this.http.post<BookingResponse>(`${environment.apiUrl}/bookings`, request);
  }

  getMine() {
    return this.http.get<BookingResponse[]>(`${environment.apiUrl}/bookings/mine`);
  }

  getAll(page: number, pageSize: number) {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<AdminBookingResponse[]>(`${environment.apiUrl}/bookings`, { params });
  }
}
