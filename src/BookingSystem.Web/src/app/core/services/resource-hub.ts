import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth';
import { SlotBookedEvent } from '../models/resource.model';

@Injectable({ providedIn: 'root' })
export class ResourceHubService {
  private readonly auth = inject(AuthService);
  private connection: HubConnection | null = null;
  private currentResourceId: number | null = null;

  private readonly slotBookedSubject = new Subject<SlotBookedEvent>();
  readonly slotBooked = this.slotBookedSubject.asObservable();

  private async ensureConnected(): Promise<HubConnection> {
    if (!this.connection) {
      this.connection = new HubConnectionBuilder()
        .withUrl(environment.hubUrl, { accessTokenFactory: () => this.auth.token() ?? '' })
        .withAutomaticReconnect()
        .build();
      this.connection.on('SlotBooked', (payload: SlotBookedEvent) => this.slotBookedSubject.next(payload));
    }

    if (this.connection.state === HubConnectionState.Disconnected) {
      await this.connection.start();
    }

    return this.connection;
  }

  async joinResourceGroup(resourceId: number): Promise<void> {
    const connection = await this.ensureConnected();
    if (this.currentResourceId !== null && this.currentResourceId !== resourceId) {
      await connection.invoke('LeaveResourceGroup', this.currentResourceId);
    }
    await connection.invoke('JoinResourceGroup', resourceId);
    this.currentResourceId = resourceId;
  }

  async leaveCurrentGroup(): Promise<void> {
    if (this.connection && this.currentResourceId !== null && this.connection.state === HubConnectionState.Connected) {
      await this.connection.invoke('LeaveResourceGroup', this.currentResourceId);
    }
    this.currentResourceId = null;
  }
}
