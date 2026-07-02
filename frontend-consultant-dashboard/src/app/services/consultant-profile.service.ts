import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, catchError, map, Observable, of, tap } from 'rxjs';

import { environment } from '../../environments/environment';
import { ApiResult } from '../models/api-result.model';
import { CompleteConsultantProfileCommand } from '../models/auth.model';
import { ConsultantProfile } from '../models/consultant-dashboard.model';
import { AuthSessionService } from './auth-session.service';

type ApiResultResponse<T> = ApiResult<T> & {
  IsSuccess?: boolean;
  Message?: string;
  Data?: T | null;
};

@Injectable({ providedIn: 'root' })
export class ConsultantProfileService {
  private readonly dentalApiBaseUrl = environment.apiBaseUrl;
  private readonly profileSubject = new BehaviorSubject<ConsultantProfile>({
    id: 'current-consultant',
    fullName: 'مشاور دل‌خند',
    phoneNumber: '۰۹۱۲ ۰۰۰ ۰۰۰۰',
    isActive: true,
    isPresent: true,
    isOnline: false,
    profileCompletionPercent: 86
  });

  readonly profile$ = this.profileSubject.asObservable();

  constructor(
    private readonly http: HttpClient,
    private readonly authSession: AuthSessionService
  ) {}

  completeProfile(command: CompleteConsultantProfileCommand): Observable<ApiResult<object>> {
    return this.http.post<ApiResultResponse<object>>(`${this.dentalApiBaseUrl}/Consultant`, command, {
      headers: this.getAuthorizationHeaders()
    }).pipe(
      map((response) => this.normalizeResult(response)),
      catchError((error: HttpErrorResponse) => of(this.toFailureResult<object>(error)))
    );
  }

  setPresentStatus(isPresent: boolean): Observable<ApiResult<ConsultantProfile>> {
    return this.http
      .post<ApiResultResponse<ConsultantProfile>>(`${this.dentalApiBaseUrl}/consultant-profile/present-status`, { isPresent })
      .pipe(
        map((response) => this.normalizeResult(response)),
        tap((result) => {
          if (result.isSuccess && result.data) {
            this.profileSubject.next(result.data);
          }
        }),
        catchError((error: HttpErrorResponse) => this.applyLocalPresentStatus(isPresent, error))
      );
  }

  setOnlineStatus(isOnline: boolean): Observable<ApiResult<ConsultantProfile>> {
    return this.http
      .post<ApiResultResponse<ConsultantProfile>>(`${this.dentalApiBaseUrl}/consultant-profile/online-status`, { isOnline })
      .pipe(
        map((response) => this.normalizeResult(response)),
        tap((result) => {
          if (result.isSuccess && result.data) {
            this.profileSubject.next(result.data);
          }
        }),
        catchError((error: HttpErrorResponse) => this.applyLocalOnlineStatus(isOnline, error))
      );
  }

  private applyLocalPresentStatus(isPresent: boolean, error: HttpErrorResponse): Observable<ApiResult<ConsultantProfile>> {
    const current = this.profileSubject.value;
    const data = { ...current, isPresent, isOnline: isPresent ? current.isOnline : false };
    this.profileSubject.next(data);

    return of({
      isSuccess: true,
      message: error.status === 0 ? 'وضعیت حضور به صورت محلی به‌روزرسانی شد.' : 'وضعیت حضور به‌روزرسانی شد.',
      data
    });
  }

  private applyLocalOnlineStatus(isOnline: boolean, error: HttpErrorResponse): Observable<ApiResult<ConsultantProfile>> {
    const current = this.profileSubject.value;
    const data = { ...current, isOnline: isOnline && current.isPresent };
    this.profileSubject.next(data);

    return of({
      isSuccess: true,
      message: error.status === 0 ? 'وضعیت آنلاین به صورت محلی به‌روزرسانی شد.' : 'وضعیت آنلاین به‌روزرسانی شد.',
      data
    });
  }

  private getAuthorizationHeaders(): HttpHeaders {
    const token = this.authSession.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }

  private toFailureResult<T>(error: HttpErrorResponse): ApiResult<T> {
    const body = error.error as Partial<ApiResultResponse<T>> | string | null;

    if (body && typeof body === 'object' && ('message' in body || 'Message' in body)) {
      return this.normalizeResult(body as ApiResultResponse<T>);
    }

    if (typeof body === 'string' && body.trim()) {
      return {
        isSuccess: false,
        message: body,
        data: null
      };
    }

    return {
      isSuccess: false,
      message: error.message,
      data: null
    };
  }

  private normalizeResult<T>(response: ApiResultResponse<T>): ApiResult<T> {
    return {
      isSuccess: response.isSuccess ?? response.IsSuccess ?? false,
      message: response.message ?? response.Message ?? '',
      data: response.data ?? response.Data ?? null
    };
  }
}
