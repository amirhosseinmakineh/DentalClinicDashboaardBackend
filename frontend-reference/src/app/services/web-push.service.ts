import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../config/api.config';
import { ApiResult } from '../models/api-result.model';
import { AuthSessionService } from './auth-session.service';

type WebPushPublicKeyResponse = ApiResult<string> & {
  Data?: string;
};

@Injectable({ providedIn: 'root' })
export class WebPushService {
  private readonly consultantApiBaseUrl = `${API_BASE_URL}/consultant`;
  private registration: ServiceWorkerRegistration | null = null;

  constructor(
    private readonly http: HttpClient,
    private readonly authSession: AuthSessionService
  ) {}

  async ensureRegistered(profileId: number): Promise<boolean> {
    if (!this.isSupported()) {
      return false;
    }

    const permission = await Notification.requestPermission();
    if (permission !== 'granted') {
      return false;
    }

    this.registration = await navigator.serviceWorker.register('/sw.js', {
      scope: '/'
    });

    await navigator.serviceWorker.ready;

    const publicKey = await this.fetchPublicKey();
    if (!publicKey) {
      return false;
    }

    const subscription = await this.registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: this.urlBase64ToUint8Array(publicKey)
    });

    await firstValueFrom(
      this.http.post<ApiResult>(
        `${this.consultantApiBaseUrl}/RegisterPushToken`,
        {
          ProfileId: profileId,
          DeviceToken: JSON.stringify(subscription.toJSON())
        },
        { headers: this.getAuthorizationHeaders() }
      )
    );

    return true;
  }

  isSupported(): boolean {
    return (
      typeof window !== 'undefined' &&
      'serviceWorker' in navigator &&
      'PushManager' in window &&
      'Notification' in window
    );
  }

  async closeRealtimeLeadNotification(leadId: number): Promise<void> {
    const registration = this.registration ?? (await navigator.serviceWorker.getRegistration());

    if (!registration) {
      return;
    }

    const tag = `realtime-lead-${leadId}`;
    const notifications = await registration.getNotifications({ tag });
    notifications.forEach((notification) => notification.close());

    if (navigator.serviceWorker.controller) {
      navigator.serviceWorker.controller.postMessage({
        type: 'CloseRealtimeLeadNotification',
        leadId
      });
    }
  }

  private async fetchPublicKey(): Promise<string | null> {
    const response = await firstValueFrom(
      this.http.get<WebPushPublicKeyResponse>(`${this.consultantApiBaseUrl}/WebPushPublicKey`)
    );

    const data = response.data ?? response.Data ?? null;
    return typeof data === 'string' && data.trim() ? data.trim() : null;
  }

  private getAuthorizationHeaders(): HttpHeaders {
    const token = this.authSession.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }

  private urlBase64ToUint8Array(base64String: string): Uint8Array<ArrayBuffer> {
    const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length) as Uint8Array<ArrayBuffer>;

    for (let index = 0; index < rawData.length; index += 1) {
      outputArray[index] = rawData.charCodeAt(index);
    }

    return outputArray;
  }
}
