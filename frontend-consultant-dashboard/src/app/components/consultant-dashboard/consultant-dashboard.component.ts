import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';

import { ConsultantStatusSnapshot, ConsultantStatusState } from '../../models/consultant-status.model';
import { AuthSessionService } from '../../services/auth-session.service';
import { ConsultantStatusService } from '../../services/consultant-status.service';

@Component({
  selector: 'app-consultant-dashboard',
  imports: [CommonModule],
  templateUrl: './consultant-dashboard.component.html',
  styleUrl: './consultant-dashboard.component.scss'
})
export class ConsultantDashboardComponent implements OnInit {
  private readonly authSession = inject(AuthSessionService);
  private readonly consultantStatusService = inject(ConsultantStatusService);
  private readonly toastr = inject(ToastrService);

  statusState: ConsultantStatusState = {
    profileId: this.authSession.getSession()?.profileId ?? 0,
    isAvailable: false,
    isOnline: false,
    pendingOfflineLeadCount: 0,
    currentScore: 0,
    canGoOnline: false,
    onlineStatusBlockReason: null,
    isLoading: true,
    isSubmittingAvailable: false,
    isSubmittingOnline: false
  };

  ngOnInit(): void {
    this.loadStatus();
    this.consultantStatusService.status$.subscribe((status) => {
      if (status) {
        this.applyStatus(status);
      }
    });
  }

  setAvailable(isAvailable: boolean): void {
    const profileId = this.getProfileId();

    if (!profileId || this.statusState.isSubmittingAvailable || (!isAvailable && this.statusState.isOnline)) {
      this.showMissingProfileIdError(profileId);
      return;
    }

    this.statusState = {
      ...this.statusState,
      profileId,
      isSubmittingAvailable: true
    };

    this.consultantStatusService
      .setAvailable({ profileId, isAvailable })
      .pipe(finalize(() => (this.statusState = { ...this.statusState, isSubmittingAvailable: false })))
      .subscribe((result) => {
        if (!result.isSuccess) {
          this.toastr.error(result.message || 'ثبت وضعیت حضور ناموفق بود.');
          return;
        }

        this.toastr.success(result.message || (isAvailable ? 'حضور شما ثبت شد.' : 'عدم حضور شما ثبت شد.'));
        this.loadStatus();
      });
  }

  setOnlineOffline(isOnline: boolean): void {
    const profileId = this.getProfileId();

    if (!profileId || this.statusState.isSubmittingOnline || (isOnline && !this.statusState.isAvailable)) {
      this.showMissingProfileIdError(profileId);
      return;
    }

    if (isOnline && !this.statusState.canGoOnline && this.statusState.onlineStatusBlockReason) {
      this.toastr.warning(this.statusState.onlineStatusBlockReason, 'امکان آنلاین شدن وجود ندارد');
      return;
    }

    this.statusState = {
      ...this.statusState,
      profileId,
      isSubmittingOnline: true
    };

    this.consultantStatusService
      .setOnlineOffline({ profileId, isOnline })
      .pipe(finalize(() => (this.statusState = { ...this.statusState, isSubmittingOnline: false })))
      .subscribe((result) => {
        if (!result.isSuccess) {
          this.toastr.error(result.message || 'ثبت وضعیت آنلاین/آفلاین ناموفق بود.');
          this.loadStatus();
          return;
        }

        this.toastr.success(result.message || (isOnline ? 'شما آنلاین شدید.' : 'شما آفلاین شدید.'));
        this.loadStatus();
      });
  }

  get onlineHint(): string | null {
    if (!this.statusState.isAvailable) {
      return 'برای آنلاین شدن ابتدا حضور خود را ثبت کنید.';
    }

    if (!this.statusState.isOnline && this.statusState.onlineStatusBlockReason) {
      return this.statusState.onlineStatusBlockReason;
    }

    if (this.statusState.isAvailable && this.statusState.isOnline) {
      return 'برای ثبت عدم حضور، ابتدا وضعیت دریافت لید را آفلاین کنید.';
    }

    return null;
  }

  private loadStatus(): void {
    const profileId = this.getProfileId();

    this.statusState = {
      ...this.statusState,
      profileId,
      isLoading: true
    };

    if (!profileId) {
      this.statusState = { ...this.statusState, isLoading: false };
      this.toastr.error('شناسه پروفایل مشاور یافت نشد. لطفاً دوباره وارد شوید.');
      return;
    }

    this.consultantStatusService
      .getStatus(profileId)
      .pipe(finalize(() => (this.statusState = { ...this.statusState, isLoading: false })))
      .subscribe((result) => {
        if (!result.isSuccess) {
          this.toastr.error(result.message || 'دریافت وضعیت مشاور ناموفق بود.');
          return;
        }

        if (result.data) {
          this.applyStatus(result.data);
        }
      });
  }

  private getProfileId(): number {
    const profileId = this.authSession.getSession()?.profileId ?? this.statusState.profileId;
    return Number.isFinite(profileId) && profileId > 0 ? profileId : 0;
  }

  private showMissingProfileIdError(profileId: number): void {
    if (!profileId) {
      this.toastr.error('شناسه پروفایل مشاور یافت نشد. لطفاً دوباره وارد شوید.');
    }
  }

  private applyStatus(status: ConsultantStatusSnapshot): void {
    this.statusState = {
      ...this.statusState,
      profileId: status.profileId,
      isAvailable: status.isAvailable,
      isOnline: status.isAvailable && status.isOnline,
      pendingOfflineLeadCount: status.pendingOfflineLeadCount,
      currentScore: status.currentScore,
      canGoOnline: status.canGoOnline,
      onlineStatusBlockReason: status.onlineStatusBlockReason ?? null
    };
  }
}
