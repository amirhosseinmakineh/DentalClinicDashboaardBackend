import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { Subscription } from 'rxjs';

import { RealtimeLeadAlert, RealtimeLeadAlertService } from '../../services/realtime-lead-alert.service';

@Component({
  selector: 'app-realtime-lead-alert',
  imports: [CommonModule],
  templateUrl: './realtime-lead-alert.component.html',
  styleUrl: './realtime-lead-alert.component.scss'
})
export class RealtimeLeadAlertComponent implements OnInit, OnDestroy {
  private readonly alertService = inject(RealtimeLeadAlertService);
  private subscription: Subscription | null = null;

  alerts: readonly RealtimeLeadAlert[] = [];

  ngOnInit(): void {
    this.alertService.initialize();
    this.subscription = this.alertService.alerts$.subscribe((alerts) => {
      this.alerts = alerts;
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  pickup(leadId: number): void {
    void this.alertService.tryPickupLead(leadId);
  }

  dismiss(leadId: number): void {
    this.alertService.dismissLead(leadId);
  }
}
