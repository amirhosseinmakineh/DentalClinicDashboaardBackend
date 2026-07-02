import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';

import { ApiResult } from '../models/api-result.model';
import { ConsultantNotification } from '../models/consultant-dashboard.model';

type ApiResultResponse<T> = ApiResult<T> & {
  IsSuccess?: boolean;
  Message?: string;
  Data?: T | null;
};

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly apiBaseUrl = 'http://localhost:5001/api';
  private readonly fallbackNotifications: ConsultantNotification[] = [
    {
      id: 'notification-1',
      title: 'لید لحظه‌ای جدید',
      message: 'لید نیلوفر رضایی به شما اختصاص داده شد. زمان تماس سه دقیقه است.',
      createdAt: new Date(Date.now() - 70_000).toISOString(),
      isRead: false,
      leadId: 'lead-rt-1'
    },
    {
      id: 'notification-2',
      title: 'یادآوری صف آفلاین',
      message: 'پیش از آنلاین شدن، لیدهای آفلاین تعیین‌تکلیف‌نشده را بررسی کنید.',
      createdAt: new Date(Date.now() - 30 * 60_000).toISOString(),
      isRead: false,
      leadId: 'lead-off-1'
    },
    {
      id: 'notification-3',
      title: 'وضعیت حضور',
      message: 'وضعیت حضور امروز شما ثبت شد.',
      createdAt: new Date(Date.now() - 2 * 60 * 60_000).toISOString(),
      isRead: true
    }
  ];

  constructor(private readonly http: HttpClient) {}

  getNotifications(): Observable<ConsultantNotification[]> {
    return this.http.get<ApiResultResponse<ConsultantNotification[]> | ConsultantNotification[]>(`${this.apiBaseUrl}/notifications`).pipe(
      map((response) => (Array.isArray(response) ? response : response.data ?? response.Data ?? [])),
      catchError((_error: HttpErrorResponse) => of(this.fallbackNotifications))
    );
  }
}
