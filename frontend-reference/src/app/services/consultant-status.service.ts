import { HttpClient, HttpErrorResponse, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';

import { API_BASE_URL } from '../config/api.config';
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

type DashboardStatusApiData = ConsultantStatusApiData & {
  canGoOnline?: boolean;
  CanGoOnline?: boolean;
  onlineStatusBlockReason?: string | null;
  OnlineStatusBlockReason?: string | null;
  pendingOfflineLeadCount?: number;
  PendingOfflineLeadCount?: number;
  currentScore?: number;
  CurrentScore?: number;
};

@Injectable({ providedIn: 'root' })
export class ConsultantStatusService {
  private readonly apiBaseUrl = `${API_BASE_URL}/consultant`;
  private readonly statusStorageKeyPrefix = 'dental_dashboard_consultant_status_';

  constructor(
    private readonly http: HttpClient,
    private readonly authSession: AuthSessionService
  ) {}

  getStatus(profileId: number): Observable<ApiResult<ConsultantStatusSnapshot>> {
    return this.http
      .get<ApiResult<DashboardStatusApiData> | DashboardStatusApiData>(`${this.apiBaseUrl}/GetDashboardStatus`, {
        headers: this.getAuthorizationHeaders(),
        params: new HttpParams().set('ProfileId', profileId)
      })
      .pipe(
        map((response) => this.normalizeGetStatusResponse(response, profileId)),
        tap((result) => {
          if (result.isSuccess && result.data) {
            this.storeStatus(result.data);
          }
        }),
        catchError((error: HttpErrorResponse) => of(this.toFailureResult(error, profileId)))
      );
  }

  setAvailable(command: SetAvailableCommand): Observable<ApiResult<ConsultantStatusSnapshot>> {
    const apiCommand = this.toSetAvailableApiCommand(command);
    const nextStatus = this.mergeWithStoredStatus(command.profileId, {
      isAvailable: command.isAvailable,
      isOnline: command.isAvailable ? undefined : false
    });

    return this.http
      .post<ConsultantStatusApiResult>(`${this.apiBaseUrl}/SetAvalableConsultant`, apiCommand, {
        headers: this.getAuthorizationHeaders()
      })
      .pipe(
        map((response) => this.normalizeResult(response, nextStatus)),
        tap((result) => {
          if (result.isSuccess && result.data) {
            this.storeStatus(result.data);
          }
        }),
        catchError((error: HttpErrorResponse) => of(this.toFailureResult(error, command.profileId)))
      );
  }

  setOnlineOffline(command: SetOnlineOfflineCommand): Observable<ApiResult<ConsultantStatusSnapshot>> {
    const apiCommand = this.toSetOnlineOfflineApiCommand(command);
    const nextStatus = this.mergeWithStoredStatus(command.profileId, { isOnline: command.isOnline });

    return this.http
      .post<ConsultantStatusApiResult>(`${this.apiBaseUrl}/SetOnlineOfflineConsultant`, apiCommand, {
        headers: this.getAuthorizationHeaders()
      })
      .pipe(
        map((response) => this.normalizeResult(response, nextStatus)),
        tap((result) => {
          if (result.isSuccess && result.data) {
            this.storeStatus(result.data);
          }
        }),
        catchError((error: HttpErrorResponse) => of(this.toFailureResult(error, command.profileId)))
      );
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

  private normalizeGetStatusResponse(
    response: ApiResult<DashboardStatusApiData> | DashboardStatusApiData,
    profileId: number
  ): ApiResult<ConsultantStatusSnapshot> {
    const fallback = this.getStoredStatus(profileId);

    if (this.isWrappedApiResult(response)) {
      return {
        isSuccess: response.isSuccess ?? true,
        message: response.message ?? '',
        data: this.normalizeStatus(response.data ?? response.Data ?? null, fallback)
      };
    }

    return {
      isSuccess: true,
      message: '',
      data: this.normalizeStatus(response, fallback)
    };
  }

  private isWrappedApiResult(
    response: ApiResult<DashboardStatusApiData> | DashboardStatusApiData
  ): response is ApiResult<DashboardStatusApiData> {
    return (
      typeof response === 'object' &&
      response !== null &&
      ('isSuccess' in response ||
        'IsSuccess' in response ||
        'message' in response ||
        'Message' in response)
    );
  }

  private normalizeResult(response: ConsultantStatusApiResult, fallback: ConsultantStatusSnapshot): ApiResult<ConsultantStatusSnapshot> {
    const data = response.data ?? response.Data ?? null;

    return {
      isSuccess: response.isSuccess ?? response.IsSuccess ?? false,
      message: response.message ?? response.Message ?? '',
      data: this.normalizeStatus(data, fallback)
    };
  }

  private toFailureResult(error: HttpErrorResponse, profileId: number): ApiResult<ConsultantStatusSnapshot> {
    const body = error.error as ConsultantStatusApiResult | string | null;


    if (body && typeof body === 'object') {
      return this.normalizeResult(body, this.getStoredStatus(profileId));
    }

    return {
      isSuccess: false,
      message: typeof body === 'string' && body.trim() ? body : error.message,
      data: this.getStoredStatus(profileId)
    };
  }

  private mergeWithStoredStatus(profileId: number, patch: Partial<ConsultantStatusSnapshot>): ConsultantStatusSnapshot {
    const current = this.getStoredStatus(profileId);

    return {
      ...current,
      ...patch,
      profileId,
      isAvailable: patch.isAvailable ?? current.isAvailable,
      isOnline: patch.isOnline ?? current.isOnline
    };
  }

  private normalizeStatus(data: ConsultantStatusApiData | null, fallback: ConsultantStatusSnapshot): ConsultantStatusSnapshot {
    return {
      profileId: Number(data?.profileId ?? data?.ProfileId ?? fallback.profileId),
      isAvailable: this.toBoolean(data?.isAvailable ?? data?.IsAvailable, fallback.isAvailable),
      isOnline: this.toBoolean(data?.isOnline ?? data?.IsOnline, fallback.isOnline),
      canGoOnline: this.toBoolean(
        (data as DashboardStatusApiData | null)?.canGoOnline ??
          (data as DashboardStatusApiData | null)?.CanGoOnline,
        fallback.canGoOnline ?? false
      ),
      onlineStatusBlockReason:
        (data as DashboardStatusApiData | null)?.onlineStatusBlockReason ??
        (data as DashboardStatusApiData | null)?.OnlineStatusBlockReason ??
        fallback.onlineStatusBlockReason ??
        null,
      pendingOfflineLeadCount: Number(
        (data as DashboardStatusApiData | null)?.pendingOfflineLeadCount ??
          (data as DashboardStatusApiData | null)?.PendingOfflineLeadCount ??
          fallback.pendingOfflineLeadCount ??
          0
      ),
      currentScore: Number(
        (data as DashboardStatusApiData | null)?.currentScore ??
          (data as DashboardStatusApiData | null)?.CurrentScore ??
          fallback.currentScore ??
          0
      )
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
      canGoOnline: false,
      onlineStatusBlockReason: null,
      pendingOfflineLeadCount: 0,
      currentScore: 0
    };
  }
}
