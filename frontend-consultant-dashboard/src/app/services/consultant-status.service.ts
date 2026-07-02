import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, catchError, map, Observable, of, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { ApiResult } from '../models/api-result.model';
import {
  ConsultantStatusApiData,
  ConsultantStatusApiResult,
  ConsultantStatusSnapshot,
  SetAvailableApiCommand,
  SetAvailableCommand,
  SetOnlineOfflineApiCommand,
  SetOnlineOfflineCommand
} from '../models/consultant-status.model';
import { AuthSessionService } from './auth-session.service';

@Injectable({ providedIn: 'root' })
export class ConsultantStatusService {
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/Consultant`;
  private readonly statusStorageKeyPrefix = 'dental_dashboard_consultant_status_';
  private readonly statusSubject = new BehaviorSubject<ConsultantStatusSnapshot | null>(null);

  readonly status$ = this.statusSubject.asObservable();

  constructor(
    private readonly http: HttpClient,
    private readonly authSession: AuthSessionService
  ) {}

  getStatus(profileId: number): Observable<ApiResult<ConsultantStatusSnapshot>> {
    return this.http
      .get<ConsultantStatusApiResult | ConsultantStatusApiData>(`${this.apiBaseUrl}/GetDashboardStatus`, {
        headers: this.getAuthorizationHeaders(),
        params: { profileId }
      })
      .pipe(
        map((response) => this.normalizeDashboardResult(response, this.getDefaultStatus(profileId))),
        tap((result) => {
          if (result.isSuccess && result.data) {
            this.storeStatus(result.data);
            this.statusSubject.next(result.data);
          }
        }),
        catchError((error: HttpErrorResponse) => of(this.toFailureResult(error, profileId)))
      );
  }

  refreshStatus(profileId: number): Observable<ApiResult<ConsultantStatusSnapshot>> {
    return this.getStatus(profileId);
  }

  setAvailable(command: SetAvailableCommand): Observable<ApiResult<ConsultantStatusSnapshot>> {
    const apiCommand = this.toSetAvailableApiCommand(command);

    return this.http
      .post<ConsultantStatusApiResult>(`${this.apiBaseUrl}/SetAvalableConsultant`, apiCommand, {
        headers: this.getAuthorizationHeaders()
      })
      .pipe(
        map((response) => this.normalizeCommandResult(response, command.profileId)),
        tap((result) => {
          if (result.isSuccess) {
            this.refreshStatus(command.profileId).subscribe();
          }
        }),
        catchError((error: HttpErrorResponse) => of(this.toFailureResult(error, command.profileId)))
      );
  }

  setOnlineOffline(command: SetOnlineOfflineCommand): Observable<ApiResult<ConsultantStatusSnapshot>> {
    const apiCommand = this.toSetOnlineOfflineApiCommand(command);

    return this.http
      .post<ConsultantStatusApiResult>(`${this.apiBaseUrl}/SetOnlineOfflineConsultant`, apiCommand, {
        headers: this.getAuthorizationHeaders()
      })
      .pipe(
        map((response) => this.normalizeCommandResult(response, command.profileId)),
        tap((result) => {
          if (result.isSuccess) {
            this.refreshStatus(command.profileId).subscribe();
          }
        }),
        catchError((error: HttpErrorResponse) => of(this.toFailureResult(error, command.profileId)))
      );
  }

  applySnapshot(status: ConsultantStatusSnapshot): void {
    this.storeStatus(status);
    this.statusSubject.next(status);
  }

  private toSetAvailableApiCommand(command: SetAvailableCommand): SetAvailableApiCommand {
    return {
      ProfileId: command.profileId,
      IsAvailable: command.isAvailable
    };
  }

  private toSetOnlineOfflineApiCommand(command: SetOnlineOfflineCommand): SetOnlineOfflineApiCommand {
    return {
      ProfileId: command.profileId,
      IsOnline: command.isOnline
    };
  }

  private getAuthorizationHeaders(): HttpHeaders {
    const token = this.authSession.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }

  private normalizeDashboardResult(
    response: ConsultantStatusApiResult | ConsultantStatusApiData,
    fallback: ConsultantStatusSnapshot
  ): ApiResult<ConsultantStatusSnapshot> {
    if (this.isWrappedResult(response)) {
      const data = response.data ?? response.Data ?? null;
      return {
        isSuccess: response.isSuccess ?? response.IsSuccess ?? Boolean(data),
        message: response.message ?? response.Message ?? '',
        data: this.normalizeStatus(data, fallback)
      };
    }

    return {
      isSuccess: true,
      message: '',
      data: this.normalizeStatus(response, fallback)
    };
  }

  private normalizeCommandResult(
    response: ConsultantStatusApiResult,
    profileId: number
  ): ApiResult<ConsultantStatusSnapshot> {
    const isSuccess = response.isSuccess ?? response.IsSuccess ?? false;
    const message = response.message ?? response.Message ?? '';

    return {
      isSuccess,
      message,
      data: isSuccess ? this.getStoredStatus(profileId) : this.getStoredStatus(profileId)
    };
  }

  private toFailureResult(error: HttpErrorResponse, profileId: number): ApiResult<ConsultantStatusSnapshot> {
    const body = error.error as ConsultantStatusApiResult | string | null;

    if (body && typeof body === 'object') {
      return {
        isSuccess: false,
        message: body.message ?? body.Message ?? 'خطا در ارتباط با سرور',
        data: this.getStoredStatus(profileId)
      };
    }

    return {
      isSuccess: false,
      message: typeof body === 'string' && body.trim() ? body : 'خطا در ارتباط با سرور',
      data: this.getStoredStatus(profileId)
    };
  }

  private isWrappedResult(value: ConsultantStatusApiResult | ConsultantStatusApiData): value is ConsultantStatusApiResult {
    return 'isSuccess' in value || 'IsSuccess' in value || 'data' in value || 'Data' in value;
  }

  private normalizeStatus(data: ConsultantStatusApiData | null, fallback: ConsultantStatusSnapshot): ConsultantStatusSnapshot {
    return {
      profileId: Number(data?.profileId ?? data?.ProfileId ?? fallback.profileId),
      isAvailable: this.toBoolean(data?.isAvailable ?? data?.IsAvailable, fallback.isAvailable),
      isOnline: this.toBoolean(data?.isOnline ?? data?.IsOnline, fallback.isOnline),
      pendingOfflineLeadCount: Number(data?.pendingOfflineLeadCount ?? data?.PendingOfflineLeadCount ?? fallback.pendingOfflineLeadCount),
      currentScore: Number(data?.currentScore ?? data?.CurrentScore ?? fallback.currentScore),
      canGoOnline: this.toBoolean(data?.canGoOnline ?? data?.CanGoOnline, fallback.canGoOnline),
      onlineStatusBlockReason: data?.onlineStatusBlockReason ?? data?.OnlineStatusBlockReason ?? fallback.onlineStatusBlockReason ?? null
    };
  }

  private toBoolean(value: unknown, fallback: boolean): boolean {
    if (typeof value === 'boolean') {
      return value;
    }

    if (typeof value === 'string') {
      const normalizedValue = value.trim().toLowerCase();

      if (['true', '1', 'yes'].includes(normalizedValue)) {
        return true;
      }

      if (['false', '0', 'no'].includes(normalizedValue)) {
        return false;
      }
    }

    if (typeof value === 'number') {
      return value === 1;
    }

    return fallback;
  }

  private getStoredStatus(profileId: number): ConsultantStatusSnapshot {
    const rawValue = localStorage.getItem(`${this.statusStorageKeyPrefix}${profileId}`);

    if (!rawValue) {
      return this.getDefaultStatus(profileId);
    }

    try {
      return this.normalizeStatus(JSON.parse(rawValue) as ConsultantStatusApiData, this.getDefaultStatus(profileId));
    } catch {
      return this.getDefaultStatus(profileId);
    }
  }

  private storeStatus(status: ConsultantStatusSnapshot): void {
    localStorage.setItem(`${this.statusStorageKeyPrefix}${status.profileId}`, JSON.stringify(status));
  }

  private getDefaultStatus(profileId: number): ConsultantStatusSnapshot {
    return {
      profileId,
      isAvailable: false,
      isOnline: false,
      pendingOfflineLeadCount: 0,
      currentScore: 0,
      canGoOnline: false,
      onlineStatusBlockReason: null
    };
  }
}
