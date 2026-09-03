import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import {
  ResourceDetailResponse,
  ResourceRequest,
  ResourceResponse,
  SlotRequest,
  SlotResponse,
} from '../models/resource.model';

@Injectable({ providedIn: 'root' })
export class ResourceService {
  constructor(private readonly http: HttpClient) {}

  getAll() {
    return this.http.get<ResourceResponse[]>(`${environment.apiUrl}/resources`);
  }

  getById(id: number, date?: string) {
    let params = new HttpParams();
    if (date) {
      params = params.set('date', date);
    }
    return this.http.get<ResourceDetailResponse>(`${environment.apiUrl}/resources/${id}`, { params });
  }

  create(request: ResourceRequest) {
    return this.http.post<ResourceResponse>(`${environment.apiUrl}/resources`, request);
  }

  update(id: number, request: ResourceRequest) {
    return this.http.put<void>(`${environment.apiUrl}/resources/${id}`, request);
  }

  delete(id: number) {
    return this.http.delete<void>(`${environment.apiUrl}/resources/${id}`);
  }

  addSlots(id: number, slots: SlotRequest[]) {
    return this.http.post<SlotResponse[]>(`${environment.apiUrl}/resources/${id}/slots`, slots);
  }
}
