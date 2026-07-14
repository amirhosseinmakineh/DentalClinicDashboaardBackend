import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';

import { API_BASE_URL } from '../config/api.config';
import { ApiResult } from '../models/api-result.model';
import { AuthSessionService } from './auth-session.service';

export type PickupLeadStatus = 'success' | 'alreadyTaken' | 'dailyLimitReached' | 'error';

export interface PickupLeadResponse {
  status: PickupLeadStatus;
  message: string;
  leadAssignmentId?: number;
  consultantProfileId?: number;
  callDeadlineAt?: string;
}

type PickupApiResult = ApiResult<{
  leadAssignmentId?: number;
  consultantProfileId?: number;
  callDeadlineAt?: string;
}> & {
  Data?: {
    leadAssignmentId?: number;
    consultantProfileId?: number;
    callDeadlineAt?: string;
  };
};

type CanPickupApiPayload = {
  canPickup?: boolean;
  CanPickup?: boolean;
  dailyLimit?: number;
  DailyLimit?: number;
  todayPickupCount?: number;
  TodayPickupCount?: number;
  message?: string | null;
  Message?: string | null;
};

type CanPickupApiResult = ApiResult<CanPickupApiPayload> & {
  Data?: CanPickupApiPayload;
};

export type CanPickupLeadStatus = 'allowed' | 'dailyLimitReached' | 'error';

export interface CanPickupLeadResult {
  status: CanPickupLeadStatus;
  canPickup: boolean;
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class RealtimeLeadPickupService {
  private readonly apiBaseUrl = API_BASE_URL;

  constructor(
    private readonly http: HttpClient,
    private readonly authSession: AuthSessionService
  ) {}

  canPickupLead(profileId: number): Observable<CanPickupLeadResult> {
    return this.http
      .get<CanPickupApiResult>(`${this.apiBaseUrl}/consultant/CanPickupLead`, {
        headers: this.getAuthorizationHeaders(),
        params: { profileId }
      })
      .pipe(
        map((response) => this.mapCanPickupResponse(response)),
        catchError((error: HttpErrorResponse) =>
          of({
            status: 'error' as const,
            canPickup: false,
            message: this.extractErrorMessage(error)
          })
        )
      );
  }

  pickupLead(leadAssignmentId: number, consultantProfileId: number): Observable<PickupLeadResponse> {
    return this.http
      .post<PickupApiResult>(
        `${this.apiBaseUrl}/LeadAssignment/${leadAssignmentId}/pickup`,
        null,
        {
          headers: this.getAuthorizationHeaders(),
          params: { consultantProfileId }
        }
      )
      .pipe(
        map((response) => ({
          status: 'success' as const,
          message: response.message ?? 'لید با موفقیت برداشته شد',
          leadAssignmentId: response.data?.leadAssignmentId ?? response.Data?.leadAssignmentId,
          consultantProfileId:
            response.data?.consultantProfileId ?? response.Data?.consultantProfileId,
          callDeadlineAt: response.data?.callDeadlineAt ?? response.Data?.callDeadlineAt
        })),
        catchError((error: HttpErrorResponse) => of(this.mapPickupError(error)))
      );
  }

  private mapCanPickupResponse(response: CanPickupApiResult): CanPickupLeadResult {
    const payload = response.data ?? response.Data;
    const canPickup = this.toBoolean(
      payload?.canPickup ?? payload?.CanPickup
    );

    if (canPickup === null) {
      return {
        status: 'error',
        canPickup: false,
        message: response.message ?? 'پاسخ سرور برای بررسی سقف لید نامعتبر بود.'
      };
    }

    if (!canPickup) {
      return {
        status: 'dailyLimitReached',
        canPickup: false,
        message:
          payload?.message ??
          payload?.Message ??
          response.message ??
          'سقف روزانه لید پر شده است. امروز دیگر نمی‌توانید لید بردارید.'
      };
    }

    return {
      status: 'allowed',
      canPickup: true
    };
  }

  private toBoolean(value: unknown): boolean | null {
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
      return value === 1 ? true : value === 0 ? false : null;
    }

    return null;
  }

  private extractErrorMessage(error: HttpErrorResponse): string {
    const body = error.error as ApiResult | string | null;

    if (body && typeof body === 'object') {
      return body.message ?? 'بررسی سقف لید ناموفق بود.';
    }

    if (typeof body === 'string' && body.trim()) {
      return body;
    }

    return error.message || 'بررسی سقف لید ناموفق بود.';
  }

  private mapPickupError(error: HttpErrorResponse): PickupLeadResponse {
    const body = error.error as ApiResult | string | null;
    const message =
      body && typeof body === 'object'
        ? body.message ?? 'عملیات ناموفق بود'
        : typeof body === 'string' && body.trim()
          ? body
          : error.message;

    if (error.status === 429) {
      return { status: 'dailyLimitReached', message };
    }

    if (error.status === 409) {
      return { status: 'alreadyTaken', message };
    }

    return { status: 'error', message };
  }

  private getAuthorizationHeaders(): HttpHeaders {
    const token = this.authSession.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }
}
