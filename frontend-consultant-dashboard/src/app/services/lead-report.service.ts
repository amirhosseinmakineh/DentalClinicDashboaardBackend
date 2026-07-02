import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';

import { environment } from '../../environments/environment';
import { ApiResult } from '../models/api-result.model';
import {
  SubmitLeadCallReportApiResult,
  SubmitLeadCallReportCommand,
  SubmitLeadCallReportResponse
} from '../models/lead-report.model';
import { AuthSessionService } from './auth-session.service';

@Injectable({ providedIn: 'root' })
export class LeadReportService {
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/Consultant`;

  constructor(
    private readonly http: HttpClient,
    private readonly authSession: AuthSessionService
  ) {}

  submitReport(command: SubmitLeadCallReportCommand): Observable<ApiResult<SubmitLeadCallReportResponse>> {
    return this.http
      .post<SubmitLeadCallReportApiResult>(`${this.apiBaseUrl}/SubmitLeadCallReport`, {
        LeadAssignmentId: command.leadAssignmentId,
        ConsultantProfileId: command.consultantProfileId,
        CallResult: command.callResult,
        ReportDescription: command.reportDescription,
        PatientCity: command.patientCity,
        PatientRegion: command.patientRegion,
        BusinessName: command.businessName,
        AttendanceProbabilityPercent: command.attendanceProbabilityPercent,
        SecondaryPhoneNumber: command.secondaryPhoneNumber
      }, {
        headers: this.getAuthorizationHeaders()
      })
      .pipe(
        map((response) => this.normalizeResult(response)),
        catchError((error: HttpErrorResponse) => of(this.toFailureResult(error)))
      );
  }

  private normalizeResult(response: SubmitLeadCallReportApiResult): ApiResult<SubmitLeadCallReportResponse> {
    const data = response.data ?? response.Data ?? null;
    const isSuccess = response.isSuccess ?? response.IsSuccess ?? false;

    return {
      isSuccess,
      message: response.message ?? response.Message ?? '',
      data: data ? this.normalizeResponse(data) : null
    };
  }

  private normalizeResponse(data: Partial<SubmitLeadCallReportResponse>): SubmitLeadCallReportResponse {
    const canCreateReservation = Boolean(
      data.canCreateReservation ?? data.shouldOpenReservationPage ?? false
    );

    return {
      leadAssignmentId: Number(data.leadAssignmentId ?? 0),
      consultantProfileId: Number(data.consultantProfileId ?? 0),
      isReportSubmitted: Boolean(data.isReportSubmitted),
      reportSubmittedAt: String(data.reportSubmittedAt ?? new Date().toISOString()),
      leadAssignmentState: data.leadAssignmentState ?? 'Assigned',
      callResult: data.callResult ?? 1,
      isConsultantOnline: Boolean(data.isConsultantOnline),
      shouldOpenReservationPage: Boolean(data.shouldOpenReservationPage ?? canCreateReservation),
      canCreateReservation,
      autoOnlineApplied: Boolean(data.autoOnlineApplied),
      autoOnlineBlockedReason: data.autoOnlineBlockedReason ?? null
    };
  }

  private toFailureResult(error: HttpErrorResponse): ApiResult<SubmitLeadCallReportResponse> {
    const body = error.error as SubmitLeadCallReportApiResult | string | null;

    if (body && typeof body === 'object') {
      return {
        isSuccess: false,
        message: body.message ?? body.Message ?? 'ثبت گزارش ناموفق بود',
        data: null
      };
    }

    return {
      isSuccess: false,
      message: typeof body === 'string' && body.trim() ? body : 'ثبت گزارش ناموفق بود',
      data: null
    };
  }

  private getAuthorizationHeaders(): HttpHeaders {
    const token = this.authSession.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }
}
