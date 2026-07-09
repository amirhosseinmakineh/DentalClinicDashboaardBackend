import { Injectable, OnDestroy, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Subject, firstValueFrom } from 'rxjs';

import { AuthSessionService } from './auth-session.service';
import { RealtimeLeadPickupService } from './realtime-lead-pickup.service';
import { WebPushService } from './web-push.service';

export interface RealtimeLeadAlert {
  leadId: number;
  title: string;
  body: string;
  isSubmitting: boolean;
  receivedAt: Date;
}

type ServiceWorkerMessage =
  | { type: 'RealtimeLead'; leadId: number; title?: string; body?: string }
  | { type: 'RealtimeLeadTaken'; leadId: number }
  | { type: 'RealtimeLeadPickup'; leadId: number }
  | { type: 'RealtimeLeadOpen'; leadId: number }
  | { type: 'OfflineLeads'; count: number };

@Injectable({ providedIn: 'root' })
export class RealtimeLeadAlertService implements OnDestroy {
  private readonly authSession = inject(AuthSessionService);
  private readonly pickupService = inject(RealtimeLeadPickupService);
  private readonly webPushService = inject(WebPushService);
  private readonly toastr = inject(ToastrService);
  private readonly router = inject(Router);

  private readonly alertsSubject = new Subject<readonly RealtimeLeadAlert[]>();
  private readonly activeAlerts = new Map<number, RealtimeLeadAlert>();
  private readonly handledLeadIds = new Set<number>();
  private readonly limitNotifiedDates = new Set<string>();
  private audioContext: AudioContext | null = null;
  private initialized = false;

  readonly alerts$ = this.alertsSubject.asObservable();

  initialize(): void {
    if (this.initialized || typeof window === 'undefined') {
      return;
    }

    this.initialized = true;
    navigator.serviceWorker?.addEventListener('message', (event: MessageEvent<ServiceWorkerMessage>) => {
      void this.handleServiceWorkerMessage(event.data);
    });
  }

  ngOnDestroy(): void {
    this.alertsSubject.complete();
    void this.audioContext?.close();
  }

  async tryPickupLead(leadId: number): Promise<void> {
    const profileId = this.getProfileId();
    if (!profileId) {
      this.toastr.error('شناسه پروفایل مشاور پیدا نشد.');
      return;
    }

    const alert = this.activeAlerts.get(leadId);
    if (alert?.isSubmitting) {
      return;
    }

    if (alert) {
      alert.isSubmitting = true;
      this.emitAlerts();
    }

    const canPickup = await firstValueFrom(this.pickupService.canPickupLead(profileId));
    if (!canPickup) {
      this.showDailyLimitNotificationOnce();
      this.dismissLead(leadId);
      return;
    }

    const result = await firstValueFrom(this.pickupService.pickupLead(leadId, profileId));

    if (result.status === 'success') {
      this.toastr.success(result.message);
      this.dismissLead(leadId);
      await this.router.navigate(['/consultant/leadManagment']);
      return;
    }

    if (result.status === 'dailyLimitReached') {
      this.showDailyLimitNotificationOnce(result.message);
      this.dismissLead(leadId);
      return;
    }

    if (result.status === 'alreadyTaken') {
      this.toastr.info(result.message || 'این لید قبلاً برداشته شده است.');
      this.dismissLead(leadId);
      return;
    }

    this.toastr.error(result.message || 'برداشتن لید ناموفق بود.');
    if (alert) {
      alert.isSubmitting = false;
      this.emitAlerts();
    }
  }

  dismissLead(leadId: number): void {
    this.activeAlerts.delete(leadId);
    this.handledLeadIds.add(leadId);
    void this.webPushService.closeRealtimeLeadNotification(leadId);
    this.emitAlerts();
  }

  private async handleServiceWorkerMessage(message: ServiceWorkerMessage | undefined): Promise<void> {
    if (!message?.type) {
      return;
    }

    if (this.authSession.getSession()?.role !== 'consultant') {
      return;
    }

    switch (message.type) {
      case 'RealtimeLead':
        await this.handleIncomingLead(message.leadId, message.title, message.body);
        break;
      case 'RealtimeLeadTaken':
        this.dismissLead(message.leadId);
        break;
      case 'RealtimeLeadPickup':
        await this.tryPickupLead(message.leadId);
        break;
      case 'RealtimeLeadOpen':
        await this.router.navigate(['/dashboard']);
        break;
      case 'OfflineLeads':
        this.toastr.info(
          message.count > 0
            ? `${message.count} لید آفلاین دارید. لطفاً بررسی کنید.`
            : 'لید آفلاین جدید دارید.',
          'لید آفلاین',
          { timeOut: 7000 }
        );
        break;
      default:
        break;
    }
  }

  private async handleIncomingLead(
    leadId: number,
    title?: string,
    body?: string
  ): Promise<void> {
    if (!leadId || this.handledLeadIds.has(leadId) || this.activeAlerts.has(leadId)) {
      return;
    }

    const profileId = this.getProfileId();
    if (!profileId) {
      return;
    }

    const canPickup = await firstValueFrom(this.pickupService.canPickupLead(profileId));
    if (!canPickup) {
      this.showDailyLimitNotificationOnce();
      await this.webPushService.closeRealtimeLeadNotification(leadId);
      return;
    }

    this.activeAlerts.set(leadId, {
      leadId,
      title: title?.trim() || 'لید جدید!',
      body: body?.trim() || 'یک لید لحظه‌ای آماده دریافت است. سریع برداریدش!',
      isSubmitting: false,
      receivedAt: new Date()
    });

    this.playAlertSound();
    this.emitAlerts();
  }

  private showDailyLimitNotificationOnce(message?: string): void {
    const todayKey = new Date().toISOString().slice(0, 10);
    if (this.limitNotifiedDates.has(todayKey)) {
      return;
    }

    this.limitNotifiedDates.add(todayKey);
    this.toastr.warning(
      message ?? 'سقف روزانه ۱۰ لید پر شده است. امروز دیگر نمی‌توانید لید بردارید.',
      'محدودیت روزانه',
      { timeOut: 8000, closeButton: true }
    );
  }

  private emitAlerts(): void {
    this.alertsSubject.next([...this.activeAlerts.values()]);
  }

  private getProfileId(): number {
    const profileId = this.authSession.getSession()?.profileId ?? 0;
    return Number.isFinite(profileId) && profileId > 0 ? profileId : 0;
  }

  private playAlertSound(): void {
    try {
      this.audioContext ??= new AudioContext();
      const context = this.audioContext;
      const oscillator = context.createOscillator();
      const gain = context.createGain();

      oscillator.type = 'square';
      oscillator.frequency.value = 880;
      gain.gain.value = 0.08;

      oscillator.connect(gain);
      gain.connect(context.destination);
      oscillator.start();

      window.setTimeout(() => {
        oscillator.stop();
      }, 180);

      window.setTimeout(() => {
        const secondOscillator = context.createOscillator();
        const secondGain = context.createGain();
        secondOscillator.type = 'square';
        secondOscillator.frequency.value = 1175;
        secondGain.gain.value = 0.08;
        secondOscillator.connect(secondGain);
        secondGain.connect(context.destination);
        secondOscillator.start();
        window.setTimeout(() => secondOscillator.stop(), 180);
      }, 220);
    } catch {
      // Browsers may block audio until user interaction; ignore silently.
    }
  }
}
