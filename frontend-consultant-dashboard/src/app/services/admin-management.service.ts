import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';

import { environment } from '../../environments/environment';
import { AdminRole, AdminUser, CreateUserCommandPayload, DeleteUserCommandPayload, RoleCommandPayload, UpdateUserCommandPayload } from '../models/admin-management.model';
import { ApiResult } from '../models/api-result.model';
import { AuthSessionService } from './auth-session.service';

type ApiResultResponse<T> = ApiResult<T> & {
  IsSuccess?: boolean;
  Message?: string;
  Data?: T | null;
};

type CollectionEnvelope<T> = {
  items?: T[];
  Items?: T[];
  data?: T[];
  Data?: T[];
};

@Injectable({ providedIn: 'root' })
export class AdminManagementService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(
    private readonly http: HttpClient,
    private readonly authSession: AuthSessionService
  ) {}

  getUsers(): Observable<ApiResult<AdminUser[]>> {
    return this.http.get<ApiResultResponse<AdminUser[]> | AdminUser[] | CollectionEnvelope<AdminUser>>(`${this.apiBaseUrl}/User`, {
      headers: this.getAuthorizationHeaders()
    }).pipe(
      map((response) => this.normalizeCollectionResult(response)),
      catchError((error: HttpErrorResponse) => of(this.toFailureResult<AdminUser[]>(error)))
    );
  }

  createUser(command: CreateUserCommandPayload): Observable<ApiResult<object>> {
    return this.http.post<ApiResultResponse<object>>(`${this.apiBaseUrl}/User`, command, {
      headers: this.getAuthorizationHeaders()
    }).pipe(
      map((response) => this.normalizeResult(response)),
      catchError((error: HttpErrorResponse) => of(this.toFailureResult<object>(error)))
    );
  }

  updateUser(command: UpdateUserCommandPayload): Observable<ApiResult<object>> {
    return this.http.put<ApiResultResponse<object>>(`${this.apiBaseUrl}/User`, command, {
      headers: this.getAuthorizationHeaders()
    }).pipe(
      map((response) => this.normalizeResult(response)),
      catchError((error: HttpErrorResponse) => of(this.toFailureResult<object>(error)))
    );
  }

  deleteUser(command: DeleteUserCommandPayload): Observable<ApiResult<object>> {
    return this.http.request<ApiResultResponse<object>>('DELETE', `${this.apiBaseUrl}/User`, {
      body: command,
      headers: this.getAuthorizationHeaders()
    }).pipe(
      map((response) => this.normalizeResult(response)),
      catchError((error: HttpErrorResponse) => of(this.toFailureResult<object>(error)))
    );
  }
getRoles(): Observable<ApiResult<AdminRole[]>> {
  return this.http.get<ApiResultResponse<AdminRole[]> | AdminRole[] | CollectionEnvelope<AdminRole>>(
    `${this.apiBaseUrl}/Role`,
    { headers: this.getAuthorizationHeaders() }
  ).pipe(
    map((response) => this.normalizeCollectionResult(response)),
    catchError((error: HttpErrorResponse) => of(this.toFailureResult<AdminRole[]>(error)))
  );
}

  createRole(command: RoleCommandPayload): Observable<ApiResult<object>> {
    return this.http.post<ApiResultResponse<object>>(`${this.apiBaseUrl}/Role`, command, {
      headers: this.getAuthorizationHeaders()
    }).pipe(
      map((response) => this.normalizeResult(response)),
      catchError((error: HttpErrorResponse) => of(this.toFailureResult<object>(error)))
    );
  }

  updateRole(command: RoleCommandPayload): Observable<ApiResult<object>> {
    return this.http.put<ApiResultResponse<object>>(`${this.apiBaseUrl}/Role`, command, {
      headers: this.getAuthorizationHeaders()
    }).pipe(
      map((response) => this.normalizeResult(response)),
      catchError((error: HttpErrorResponse) => of(this.toFailureResult<object>(error)))
    );
  }

  deleteRole(command: RoleCommandPayload): Observable<ApiResult<object>> {
    return this.http.request<ApiResultResponse<object>>('DELETE', `${this.apiBaseUrl}/Role`, {
      body: command,
      headers: this.getAuthorizationHeaders()
    }).pipe(
      map((response) => this.normalizeResult(response)),
      catchError((error: HttpErrorResponse) => of(this.toFailureResult<object>(error)))
    );
  }

  private getAuthorizationHeaders(): HttpHeaders {
    const token = this.authSession.getToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }

  private normalizeCollectionResult<T>(response: ApiResultResponse<T[]> | T[] | CollectionEnvelope<T>): ApiResult<T[]> {
    if (Array.isArray(response)) {
      return { isSuccess: true, message: '', data: response };
    }

    const result = response as ApiResultResponse<T[]>;
    const directData = result.data ?? result.Data;

    if (Array.isArray(directData)) {
      return this.normalizeResult({ ...result, data: directData });
    }

    const envelope = (result.data ?? result.Data ?? response) as CollectionEnvelope<T>;
    const items = envelope.items ?? envelope.Items ?? envelope.data ?? envelope.Data ?? [];

    return {
      isSuccess: result.isSuccess ?? result.IsSuccess ?? true,
      message: result.message ?? result.Message ?? '',
      data: items
    };
  }

  private normalizeResult<T>(response: ApiResultResponse<T>): ApiResult<T> {
    return {
      isSuccess: response.isSuccess ?? response.IsSuccess ?? true,
      message: response.message ?? response.Message ?? 'عملیات با موفقیت انجام شد.',
      data: response.data ?? response.Data ?? null
    };
  }

  private toFailureResult<T>(error: HttpErrorResponse): ApiResult<T> {
    const body = error.error as Partial<ApiResultResponse<T>> | string | null;

    if (body && typeof body === 'object' && ('message' in body || 'Message' in body)) {
      return this.normalizeResult(body as ApiResultResponse<T>);
    }

    if (typeof body === 'string' && body.trim()) {
      return { isSuccess: false, message: body, data: null };
    }

    return { isSuccess: false, message: error.message, data: null };
  }
}
