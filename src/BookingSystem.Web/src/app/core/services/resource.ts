import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { ResourceResponse } from '../models/resource.model';

@Injectable({ providedIn: 'root' })
export class ResourceService {
  constructor(private readonly http: HttpClient) {}

  getAll() {
    return this.http.get<ResourceResponse[]>(`${environment.apiUrl}/resources`);
  }
}
