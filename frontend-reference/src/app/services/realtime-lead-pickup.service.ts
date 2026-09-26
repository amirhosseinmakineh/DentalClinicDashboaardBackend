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

type CanPickupApiResult = ApiResult<{
  canPickup: boolean;
  dailyLimit: number;
  message?: string | null;
}> & {
  Data?: {
    canPickup: boolean;
    dailyLimit: number;
    message?: string | null;
  };
};

@Injectable({ providedIn: 'root' })
export class RealtimeLeadPickupService {
  private readonly apiBaseUrl = API_BASE_URL;

  constructor(
    private readonly http: HttpClient,
    private readonly authSession: AuthSessionService
  ) {}

  canPickupLead(profileId: number): Observable<boolean> {
    return this.http
      .get<CanPickupApiResult>(`${this.apiBaseUrl}/consultant/CanPickupLead`, {
        headers: this.getAuthorizationHeaders(),
        params: { profileId }
      })
      .pipe(
        map((response) => response.data?.canPickup ?? response.Data?.canPickup ?? false),
        catchError(() => of(false))
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
