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

  // Serializes join/leave/disconnect so a reconnect callback or a fast
  // route change can never interleave with an in-flight hub invocation.
  private opQueue: Promise<unknown> = Promise.resolve();

  private enqueue<T>(fn: () => Promise<T>): Promise<T> {
    const result = this.opQueue.then(fn, fn);
    this.opQueue = result.then(
      () => undefined,
      () => undefined,
    );
    return result;
  }

  private async ensureConnected(): Promise<HubConnection> {
    if (!this.connection) {
      const connection = new HubConnectionBuilder()
        .withUrl(environment.hubUrl, { accessTokenFactory: () => this.auth.token() ?? '' })
        .withAutomaticReconnect()
        .build();
      connection.on('SlotBooked', (payload: SlotBookedEvent) => this.slotBookedSubject.next(payload));
      connection.onreconnected(() => {
        this.enqueue(async () => {
          if (this.currentResourceId !== null) {
            await connection.invoke('JoinResourceGroup', this.currentResourceId);
          }
        }).catch((err) => console.error('Failed to rejoin resource group after reconnect', err));
      });
      this.connection = connection;
    }

    if (this.connection.state === HubConnectionState.Disconnected) {
      await this.startWithRetry(this.connection);
    }

    return this.connection;
  }

  // withAutomaticReconnect only retries drops on an already-established connection -
  // it never retries a failed *initial* start(), e.g. an Azure cold start. Retry that
  // ourselves so a transient failure doesn't kill real-time updates for the rest of
  // the session; each attempt still surfaces the last error to the caller if it never
  // succeeds, and the next join attempt will retry again from a clean Disconnected state.
  private static readonly START_RETRY_DELAYS_MS = [0, 1000, 3000, 6000];

  private async startWithRetry(connection: HubConnection): Promise<void> {
    let lastError: unknown;
    for (const delay of ResourceHubService.START_RETRY_DELAYS_MS) {
      if (delay > 0) {
        await new Promise((resolve) => setTimeout(resolve, delay));
      }
      try {
        await connection.start();
        return;
      } catch (err) {
        lastError = err;
      }
    }
    throw lastError;
  }

  async joinResourceGroup(resourceId: number): Promise<void> {
    return this.enqueue(async () => {
      const connection = await this.ensureConnected();
      if (this.currentResourceId !== null && this.currentResourceId !== resourceId) {
        await connection.invoke('LeaveResourceGroup', this.currentResourceId);
      }
      await connection.invoke('JoinResourceGroup', resourceId);
      this.currentResourceId = resourceId;
    });
  }

  async leaveCurrentGroup(): Promise<void> {
    return this.enqueue(async () => {
      if (this.connection && this.currentResourceId !== null && this.connection.state === HubConnectionState.Connected) {
        await this.connection.invoke('LeaveResourceGroup', this.currentResourceId);
      }
      this.currentResourceId = null;
    });
  }

  async disconnect(): Promise<void> {
    return this.enqueue(async () => {
      const connection = this.connection;
      this.connection = null;
      this.currentResourceId = null;
      if (connection) {
        await connection.stop();
      }
    });
  }
}
