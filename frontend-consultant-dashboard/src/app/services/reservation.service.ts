import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';

import { environment } from '../../environments/environment';
import { ApiResult } from '../models/api-result.model';
import {
  CreateReservationApiResult,
  CreateReservationCommand,
  CreateReservationResponse
} from '../models/reservation.model';
import { AuthSessionService } from './auth-session.service';

@Injectable({ providedIn: 'root' })
export class ReservationService {
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/Reservation`;

  constructor(
    private readonly http: HttpClient,
    private readonly authSession: AuthSessionService
  ) {}

  createReservation(command: CreateReservationCommand): Observable<ApiResult<CreateReservationResponse>> {
    return this.http
      .post<CreateReservationApiResult>(`${this.apiBaseUrl}`, {
        LeadAssignmentId: command.leadAssignmentId,
        ConsultantProfileId: command.consultantProfileId,
        ReservationAt: command.reservationAt,
        Description: command.description
      }, {
        headers: this.getAuthorizationHeaders()
      })
      .pipe(
        map((response) => this.normalizeResult(response)),
        catchError((error: HttpErrorResponse) => of(this.toFailureResult(error)))
      );
  }

  private normalizeResult(response: CreateReservationApiResult): ApiResult<CreateReservationResponse> {
    const data = response.data ?? response.Data ?? null;
    const isSuccess = response.isSuccess ?? response.IsSuccess ?? false;

    return {
      isSuccess,
      message: response.message ?? response.Message ?? '',
      data: data
        ? {
            id: Number(data.id ?? 0),
            leadAssignmentId: Number(data.leadAssignmentId ?? 0),
            consultantProfileId: Number(data.consultantProfileId ?? 0),
            reservationAt: String(data.reservationAt ?? ''),
            patientName: data.patientName,
            patientPhoneNumber: data.patientPhoneNumber
          }
        : null
    };
  }

  private toFailureResult(error: HttpErrorResponse): ApiResult<CreateReservationResponse> {
    const body = error.error as CreateReservationApiResult | string | null;

    if (body && typeof body === 'object') {
      return {
        isSuccess: false,
        message: body.message ?? body.Message ?? 'ثبت رزرو ناموفق بود',
        data: null
      };
    }

    return {
      isSuccess: false,
      message: typeof body === 'string' && body.trim() ? body : 'ثبت رزرو ناموفق بود',
      data: null
    };
  }

  private getAuthorizationHeaders(): HttpHeaders {
    const token = this.authSession.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }
}
