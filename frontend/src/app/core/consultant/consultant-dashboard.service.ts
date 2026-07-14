import { HttpClient, HttpHeaders, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable, catchError, map, throwError } from "rxjs";
import { AuthService } from "../auth/auth.service";
import { environment } from "../../../environments/environment";

export interface ApiCommandResponse<T = unknown> {
  isSuccess: boolean;
  message: string;
  data?: T;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  raw?: unknown;
  source?: unknown;
}

export interface CompleteConsultantProfileRequest {
  profileId: number;
  userId?: string;
  nationalityCode: string;
  address: string;
  isCompleteProfile: boolean;
}

export interface AvailabilityRequest {
  profileId: number;
  isAvailable: boolean;
}

export interface OnlineRequest {
  profileId: number;
  isOnline: boolean;
  isOffline: boolean;
}

export interface RegisterPushTokenRequest {
  profileId: number;
  deviceToken: string;
}

export interface ConsultantDashboardStatus {
  profileId: number;
  isAvailable: boolean;
  isOnline: boolean;
  lastOnlineAt: string | null;
  lastOfflineAt: string | null;
  canGoOnline: boolean;
  onlineStatusBlockReason: string | null;
  raw?: unknown;
}

export interface BroadcastRealtimeLeadItem {
  leadAssignmentId?: number;
  LeadAssignmentId?: number;
  userName?: string | null;
  UserName?: string | null;
  phoneNumber?: string | null;
  PhoneNumber?: string | null;
  createdAt?: string;
  CreatedAt?: string;
}

export interface BroadcastRealtimeLeadsResponse {
  canReceive?: boolean;
  CanReceive?: boolean;
  blockReason?: string | null;
  BlockReason?: string | null;
  leads?: BroadcastRealtimeLeadItem[];
  Leads?: BroadcastRealtimeLeadItem[];
}

export interface ConsultantLead {
  id?: number;
  Id?: number;
  leadAssignmentId?: number;
  LeadAssignmentId?: number;
  userName?: string | null;
  UserName?: string | null;
  fullName?: string | null;
  FullName?: string | null;
  firstName?: string | null;
  FirstName?: string | null;
  lastName?: string | null;
  LastName?: string | null;
  phoneNumber?: string | null;
  PhoneNumber?: string | null;
  mobile?: string | null;
  Mobile?: string | null;
  userPhoneNumber?: string | null;
  UserPhoneNumber?: string | null;
  leadPhoneNumber?: string | null;
  LeadPhoneNumber?: string | null;
  leadAssignmentState?: number | null;
  LeadAssignmentState?: number | null;
  state?: number | null;
  State?: number | null;
  status?: number | null;
  Status?: number | null;
  leadAssignmentType?: number | null;
  LeadAssignmentType?: number | null;
  type?: number | null;
  Type?: number | null;
  assignmentType?: number | null;
  AssignmentType?: number | null;
  requiresThreeMinuteCall?: boolean | null;
  RequiresThreeMinuteCall?: boolean | null;
  callDeadlineAt?: string | null;
  CallDeadlineAt?: string | null;
  callInitiatedAt?: string | null;
  CallInitiatedAt?: string | null;
  isReportSubmitted?: boolean | null;
  IsReportSubmitted?: boolean | null;
  reportSubmittedAt?: string | null;
  ReportSubmittedAt?: string | null;
  contactedAt?: string | null;
  ContactedAt?: string | null;
  callResult?: number | null;
  CallResult?: number | null;
  reportDescription?: string | null;
  ReportDescription?: string | null;
  patientCity?: string | null;
  PatientCity?: string | null;
  patientRegion?: string | null;
  PatientRegion?: string | null;
  businessName?: string | null;
  BusinessName?: string | null;
  attendanceProbabilityPercent?: number | null;
  AttendanceProbabilityPercent?: number | null;
  secondaryPhoneNumber?: string | null;
  SecondaryPhoneNumber?: string | null;
  hasActiveReservation?: boolean | null;
  HasActiveReservation?: boolean | null;
  user?: LeadPerson | null;
  User?: LeadPerson | null;
  lead?: LeadPerson | null;
  Lead?: LeadPerson | null;
}

export interface LeadFilters {
  profileId: number;
  leadAssignmentState?: number | null;
  leadAssignmentType?: number | null;
  hasSubmittedReport?: boolean | null;
  pageNumber: number;
  pageSize: number;
}

export interface SubmitLeadCallReportRequest {
  leadAssignmentId: number;
  consultantProfileId: number;
  callResult: number;
  reportDescription: string;
  patientCity: string;
  patientRegion: string;
  attendanceProbabilityPercent?: number;
  secondaryPhoneNumber?: string | null;
}

export interface LeadCallReportResponse {
  leadAssignmentId: number;
  consultantProfileId: number;
  isReportSubmitted: boolean;
  reportSubmittedAt?: string;
  leadAssignmentState: number;
  callResult: number;
  isConsultantOnline: boolean;
  shouldOpenReservationPage?: boolean;
}

export interface ExpireLeadNoCallRequest {
  leadAssignmentId: number;
  consultantProfileId: number;
}

export interface RecordLeadCallInitiatedRequest {
  leadAssignmentId: number;
  consultantProfileId: number;
}

export interface RecordLeadCallInitiatedResponse {
  leadAssignmentId: number;
  consultantProfileId: number;
  callInitiatedAt: string;
}

export interface UpdateLeadCallReportRequest {
  leadAssignmentId: number;
  consultantProfileId: number;
  callResult: number;
  reportDescription: string;
  patientCity: string;
  patientRegion: string;
  attendanceProbabilityPercent?: number;
  secondaryPhoneNumber?: string | null;
}

export interface ExpireLeadNoCallResponse {
  leadAssignmentId: number;
  consultantProfileId: number;
  leadAssignmentState: number;
  isConsultantOnline: boolean;
}

export interface CreateReservationRequest {
  leadAssignmentId: number;
  consultantProfileId: number;
  reservationAt: string;
  patientCity: string;
  patientRegion: string;
  attendanceProbabilityPercent: number;
  attendancePrediction: string;
  secondaryPhoneNumber?: string | null;
  description?: string | null;
}

export interface ConfirmAttendanceRequest {
  reservationId: number;
  consultantProfileId: number;
  patientAttended: boolean;
  note: string | null;
}

export interface ConsultantReservation {
  id?: number;
  Id?: number;
  reservationId?: number;
  ReservationId?: number;
  leadAssignmentId?: number;
  LeadAssignmentId?: number;
  consultantProfileId?: number;
  ConsultantProfileId?: number;
  patientUserId?: string | null;
  PatientUserId?: string | null;
  requiresPatientProfile?: boolean;
  RequiresPatientProfile?: boolean;
  reservationAt?: string;
  ReservationAt?: string;
  patientName?: string;
  PatientName?: string;
  patientPhoneNumber?: string;
  PatientPhoneNumber?: string;
  secondaryPhoneNumber?: string | null;
  SecondaryPhoneNumber?: string | null;
  patientCity?: string | null;
  PatientCity?: string | null;
  attendanceProbabilityPercent?: number | null;
  AttendanceProbabilityPercent?: number | null;
  attendancePrediction?: string | null;
  AttendancePrediction?: string | null;
  attendanceConfirmationStatus?: number | null;
  AttendanceConfirmationStatus?: number | null;
  consultantAttendanceConfirmedAt?: string | null;
  ConsultantAttendanceConfirmedAt?: string | null;
  consultantSaysPatientAttended?: boolean | null;
  ConsultantSaysPatientAttended?: boolean | null;
  consultantAttendanceNote?: string | null;
  ConsultantAttendanceNote?: string | null;
  secretaryReviewedAt?: string | null;
  SecretaryReviewedAt?: string | null;
  secretaryUserId?: string | null;
  SecretaryUserId?: string | null;
  secretaryApprovedConsultantConfirmation?: boolean | null;
  SecretaryApprovedConsultantConfirmation?: boolean | null;
  secretaryReviewNote?: string | null;
  SecretaryReviewNote?: string | null;
  isAttendanceScoreApplied?: boolean | null;
  IsAttendanceScoreApplied?: boolean | null;
  attendanceScoreValue?: number | null;
  AttendanceScoreValue?: number | null;
  attendanceScoreAppliedAt?: string | null;
  AttendanceScoreAppliedAt?: string | null;
  isDueForConsultantConfirmation?: boolean | null;
  IsDueForConsultantConfirmation?: boolean | null;
  consultantName?: string | null;
  ConsultantName?: string | null;
  description?: string | null;
  Description?: string | null;
  isCanceled?: boolean;
  IsCanceled?: boolean;
}

export interface CompletePatientProfileRequest {
  reservationId: number;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  passwordHash: string;
  avatarImageName?: string | null;
  gender: number;
  birthDate: string;
  address: string;
  emergencyPhoneNumber?: string | null;
  insuranceName?: string | null;
  notes?: string | null;
}

export interface CompletePatientProfileResponse {
  reservationId: number;
  patientUserId: string;
  patientProfileId: number;
  leadAssignmentId: number;
  consultantProfileId: number;
  reservationAt: string;
  patientName: string;
  patientPhoneNumber: string;
  isCompleteProfile: boolean;
  roleName: string;
}

export interface AddPatientLeadRequest {
  consultantProfileId: number;
  userName: string;
  phoneNumber: string;
  patientCity?: string;
  patientRegion?: string;
  secondaryPhoneNumber?: string;
  reportDescription?: string;
}

export interface AddPatientLeadResponse {
  leadAssignmentId: number;
  consultantProfileId: number;
  userName: string;
  phoneNumber: string;
  assignmentType: number;
  leadAssignmentState: number;
}

export interface ReservationFilters {
  consultantProfileId: number;
  from?: string;
  to?: string;
  includeCanceled?: boolean;
  onlySecretaryReviewed?: boolean;
  pageNumber: number;
  pageSize: number;
}

interface LeadPerson {
  userName?: string | null;
  UserName?: string | null;
  fullName?: string | null;
  FullName?: string | null;
  name?: string | null;
  Name?: string | null;
  firstName?: string | null;
  FirstName?: string | null;
  lastName?: string | null;
  LastName?: string | null;
  phoneNumber?: string | null;
  PhoneNumber?: string | null;
  mobile?: string | null;
  Mobile?: string | null;
}

@Injectable({ providedIn: "root" })
export class ConsultantDashboardService {
  private readonly apiBaseUrl = environment.apiBaseUrl;

  constructor(
    private http: HttpClient,
    private auth: AuthService,
  ) {}

  completeProfile(
    payload: CompleteConsultantProfileRequest,
  ): Observable<ApiCommandResponse<number>> {
    return this.http
      .post<ApiCommandResponse<number>>(
        `${this.apiBaseUrl}/Consultant`,
        payload,
        {
          headers: this.authHeaders(),
        },
      )
      .pipe(this.ensureCommandSucceeded("تکمیل پروفایل مشاور انجام نشد"));
  }

  setAvailability(
    payload: AvailabilityRequest,
  ): Observable<ApiCommandResponse> {
    return this.http
      .post<ApiCommandResponse>(
        `${this.apiBaseUrl}/Consultant/SetAvalableConsultant`,
        payload,
        {
          headers: this.authHeaders(),
        },
      )
      .pipe(this.ensureCommandSucceeded("ثبت حضور انجام نشد"));
  }

  setOnlineStatus(payload: OnlineRequest): Observable<ApiCommandResponse> {
    return this.http
      .post<ApiCommandResponse>(
        `${this.apiBaseUrl}/Consultant/SetOnlineOfflineConsultant`,
        payload,
        {
          headers: this.authHeaders(),
        },
      )
      .pipe(this.ensureCommandSucceeded("تغییر وضعیت آنلاین انجام نشد"));
  }

  registerPushToken(
    payload: RegisterPushTokenRequest,
  ): Observable<ApiCommandResponse> {
    return this.http
      .post<ApiCommandResponse>(
        `${this.apiBaseUrl}/Consultant/RegisterPushToken`,
        payload,
        {
          headers: this.authHeaders(),
        },
      )
      .pipe(this.ensureCommandSucceeded("ثبت توکن نوتیفیکیشن انجام نشد"));
  }

  sendTestPushNotification(
    payload: RegisterPushTokenRequest,
  ): Observable<ApiCommandResponse> {
    return this.http
      .post<ApiCommandResponse>(
        `${this.apiBaseUrl}/Consultant/SendTestPushNotification`,
        payload,
        {
          headers: this.authHeaders(),
        },
      )
      .pipe(this.ensureCommandSucceeded("ارسال نوتیفیکیشن تست انجام نشد"));
  }

  getDashboardStatus(profileId: number): Observable<ConsultantDashboardStatus> {
    return this.http
      .get<unknown>(`${this.apiBaseUrl}/Consultant/GetDashboardStatus`, {
        headers: this.authHeaders(),
        params: this.toParams({ profileId }),
      })
      .pipe(
        map((response) => this.normalizeDashboardStatus(response)),
        catchError((error) =>
          throwError(() =>
            this.toUserFacingError(error, "دریافت وضعیت داشبورد انجام نشد"),
          ),
        ),
      );
  }

  getBroadcastRealtimeLeads(
    profileId: number,
  ): Observable<BroadcastRealtimeLeadsResponse> {
    return this.http
      .get<unknown>(`${this.apiBaseUrl}/Consultant/GetBroadcastRealtimeLeads`, {
        headers: this.authHeaders(),
        params: this.toParams({ profileId }),
      })
      .pipe(
        map((response) => this.normalizeBroadcastRealtimeLeads(response)),
        catchError((error) =>
          throwError(() =>
            this.toUserFacingError(
              error,
              "دریافت لیدهای لحظه‌ای در دسترس انجام نشد",
            ),
          ),
        ),
      );
  }

  getLeads(
    filters: LeadFilters,
  ): Observable<PaginatedResponse<ConsultantLead>> {
    return this.http
      .get<unknown>(`${this.apiBaseUrl}/Consultant/GetLeads`, {
        headers: this.authHeaders(),
        params: this.toParams(filters),
      })
      .pipe(
        map((response) =>
          this.normalizePaginatedResponse<ConsultantLead>(response, filters),
        ),
        catchError((error) =>
          throwError(() =>
            this.toUserFacingError(error, "دریافت لیدها انجام نشد"),
          ),
        ),
      );
  }

  createConsultantPatientLead(
    payload: AddPatientLeadRequest,
  ): Observable<ApiCommandResponse<AddPatientLeadResponse>> {
    return this.http
      .post<ApiCommandResponse<AddPatientLeadResponse>>(
        `${this.apiBaseUrl}/Consultant/CreateConsultantPatientLead`,
        payload,
        {
          headers: this.authHeaders(),
        },
      )
      .pipe(this.ensureCommandSucceeded("ثبت بیمار انجام نشد"));
  }

  addPatientLead(
    payload: AddPatientLeadRequest,
  ): Observable<ApiCommandResponse<AddPatientLeadResponse>> {
    return this.createConsultantPatientLead(payload);
  }

  submitLeadCallReport(
    payload: SubmitLeadCallReportRequest,
  ): Observable<ApiCommandResponse<LeadCallReportResponse>> {
    return this.http
      .post<ApiCommandResponse<LeadCallReportResponse>>(
        `${this.apiBaseUrl}/Consultant/SubmitLeadCallReport`,
        payload,
        {
          headers: this.authHeaders(),
        },
      )
      .pipe(this.ensureCommandSucceeded("ثبت گزارش تماس انجام نشد"));
  }

  expireLeadNoCall(
    payload: ExpireLeadNoCallRequest,
  ): Observable<ApiCommandResponse<ExpireLeadNoCallResponse>> {
    return this.http
      .post<ApiCommandResponse<ExpireLeadNoCallResponse>>(
        `${this.apiBaseUrl}/Consultant/ExpireLeadNoCall`,
        payload,
        {
          headers: this.authHeaders(),
        },
      )
      .pipe(this.ensureCommandSucceeded("منقضی کردن لید انجام نشد"));
  }

  recordLeadCallInitiated(
    payload: RecordLeadCallInitiatedRequest,
  ): Observable<ApiCommandResponse<RecordLeadCallInitiatedResponse>> {
    return this.http
      .post<ApiCommandResponse<RecordLeadCallInitiatedResponse>>(
        `${this.apiBaseUrl}/Consultant/RecordLeadCallInitiated`,
        payload,
        {
          headers: this.authHeaders(),
        },
      )
      .pipe(this.ensureCommandSucceeded("ثبت شروع تماس انجام نشد"));
  }

  updateLeadCallReport(
    payload: UpdateLeadCallReportRequest,
  ): Observable<ApiCommandResponse<LeadCallReportResponse>> {
    return this.http
      .post<ApiCommandResponse<LeadCallReportResponse>>(
        `${this.apiBaseUrl}/Consultant/UpdateLeadCallReport`,
        payload,
        {
          headers: this.authHeaders(),
        },
      )
      .pipe(this.ensureCommandSucceeded("ویرایش گزارش تماس انجام نشد"));
  }

  createReservation(
    payload: CreateReservationRequest,
  ): Observable<ApiCommandResponse<ConsultantReservation>> {
    return this.http
      .post<ApiCommandResponse<ConsultantReservation>>(
        `${this.apiBaseUrl}/Reservation`,
        payload,
        {
          headers: this.authHeaders(),
        },
      )
      .pipe(this.ensureCommandSucceeded("ثبت رزرو انجام نشد"));
  }

  getDueConfirmations(
    consultantProfileId: number,
  ): Observable<ConsultantReservation[]> {
    return this.http
      .get<unknown>(`${this.apiBaseUrl}/Reservation/DueConfirmations`, {
        headers: this.authHeaders(),
        params: this.toParams({ consultantProfileId }),
      })
      .pipe(
        map((response) =>
          this.readItems<ConsultantReservation>(
            this.unwrapResponseData(response),
          ),
        ),
        catchError((error) =>
          throwError(() =>
            this.toUserFacingError(
              error,
              "دریافت تایید حضورهای لازم انجام نشد",
            ),
          ),
        ),
      );
  }

  confirmAttendance(
    payload: ConfirmAttendanceRequest,
  ): Observable<ApiCommandResponse<ConsultantReservation>> {
    return this.http
      .post<ApiCommandResponse<ConsultantReservation>>(
        `${this.apiBaseUrl}/Reservation/ConfirmAttendance`,
        payload,
        {
          headers: this.authHeaders(),
        },
      )
      .pipe(this.ensureCommandSucceeded("ثبت تایید حضور انجام نشد"));
  }

  completePatientProfile(
    payload: CompletePatientProfileRequest,
  ): Observable<ApiCommandResponse<CompletePatientProfileResponse>> {
    return this.http
      .post<ApiCommandResponse<CompletePatientProfileResponse>>(
        `${this.apiBaseUrl}/Reservation/CompletePatientProfile`,
        payload,
        {
          headers: this.authHeaders(),
        },
      )
      .pipe(this.ensureCommandSucceeded("تشکیل پرونده بیمار انجام نشد"));
  }

  getReservations(
    filters: ReservationFilters,
  ): Observable<PaginatedResponse<ConsultantReservation>> {
    return this.http
      .get<unknown>(
        `${this.apiBaseUrl}/Reservation/GetConsultantReservations`,
        {
          headers: this.authHeaders(),
          params: this.toParams(filters),
        },
      )
      .pipe(
        map((response) =>
          this.normalizePaginatedResponse<ConsultantReservation>(
            response,
            filters,
          ),
        ),
        catchError((error) =>
          throwError(() =>
            this.toUserFacingError(error, "دریافت رزروها انجام نشد"),
          ),
        ),
      );
  }

  private authHeaders(): HttpHeaders {
    const token = this.auth.authToken();
    const baseHeaders: Record<string, string> = { Accept: "application/json" };
    if (token) baseHeaders["Authorization"] = `Bearer ${token}`;
    return new HttpHeaders(baseHeaders);
  }

  private toParams(source: object): HttpParams {
    let params = new HttpParams();

    Object.entries(source as Record<string, unknown>).forEach(
      ([key, value]) => {
        if (value === null || value === undefined || value === "") return;
        params = params.set(key, String(value));
      },
    );

    return params;
  }

  private ensureCommandSucceeded<T>(fallback: string) {
    return (source: Observable<ApiCommandResponse<T>>) =>
      source.pipe(
        map((response) => this.normalizeCommandResponse(response, fallback)),
        catchError((error) =>
          throwError(() => this.toUserFacingError(error, fallback)),
        ),
      );
  }

  private normalizeDashboardStatus(
    response: unknown,
  ): ConsultantDashboardStatus {
    const source = this.unwrapResponseData(response);

    return {
      profileId:
        this.readNumber(source, "profileId", "consultantProfileId") ?? 0,
      isAvailable:
        this.readBoolean(
          source,
          "isAvailable",
          "IsAvailable",
          "available",
          "consultantIsAvailable",
        ) ?? false,
      isOnline:
        this.readBoolean(
          source,
          "isOnline",
          "IsOnline",
          "online",
          "consultantIsOnline",
          "isConsultantOnline",
          "IsConsultantOnline",
        ) ?? false,
      lastOnlineAt: this.readString(source, "lastOnlineAt") ?? null,
      lastOfflineAt: this.readString(source, "lastOfflineAt") ?? null,
      canGoOnline: this.readBoolean(source, "canGoOnline") ?? false,
      onlineStatusBlockReason:
        this.readString(source, "onlineStatusBlockReason", "blockReason") ??
        null,
      raw: response,
    };
  }

  private normalizeBroadcastRealtimeLeads(
    response: unknown,
  ): BroadcastRealtimeLeadsResponse {
    const source = this.unwrapResponseData(response);
    const leads = this.readItems<BroadcastRealtimeLeadItem>(source);
    const nestedLeads = this.readItems<BroadcastRealtimeLeadItem>(
      this.readValue(source, "leads", "Leads"),
    );
    const resolvedLeads = leads.length ? leads : nestedLeads;

    return {
      canReceive:
        this.readBoolean(source, "canReceive", "CanReceive") ?? false,
      blockReason:
        this.readString(source, "blockReason", "BlockReason") ?? null,
      leads: resolvedLeads,
    };
  }

  private normalizePaginatedResponse<T>(
    response: unknown,
    filters: { pageNumber: number; pageSize: number },
  ): PaginatedResponse<T> {
    const source = this.unwrapResponseData(response);
    const items = this.readItems<T>(source);
    const totalCount =
      this.readNumber(source, "totalCount", "total", "count", "recordsTotal") ??
      this.readNumber(
        response,
        "totalCount",
        "total",
        "count",
        "recordsTotal",
      ) ??
      items.length;
    const pageSize =
      this.readNumber(source, "pageSize", "take", "limit") ??
      this.readNumber(response, "pageSize", "take", "limit") ??
      filters.pageSize;
    const pageNumber =
      this.readNumber(source, "pageNumber", "page", "currentPage") ??
      this.readNumber(response, "pageNumber", "page", "currentPage") ??
      filters.pageNumber;
    const totalPages =
      this.readNumber(source, "totalPages", "pages", "pageCount") ??
      this.readNumber(response, "totalPages", "pages", "pageCount") ??
      Math.ceil(totalCount / Math.max(1, pageSize));

    return {
      items,
      totalCount,
      pageNumber,
      pageSize,
      totalPages: Math.max(1, totalPages),
      raw: response,
      source,
    };
  }

  private unwrapResponseData(response: unknown): unknown {
    if (Array.isArray(response)) return response;
    if (!this.isRecord(response)) return response;

    const payload = this.readValue(
      response,
      "data",
      "result",
      "value",
      "payload",
    );
    if (payload !== null && payload !== undefined) {
      return payload;
    }

    return response;
  }

  private readItems<T>(source: unknown): T[] {
    if (Array.isArray(source)) return source as T[];
    if (!this.isRecord(source)) return [];

    for (const key of [
      "items",
      "data",
      "result",
      "values",
      "records",
      "list",
    ]) {
      const value = this.readValue(source, key);
      if (Array.isArray(value)) return value as T[];
      const nestedItems = this.readItems<T>(value);
      if (nestedItems.length) return nestedItems;
    }

    const firstArray = Object.values(source).find(Array.isArray);
    return Array.isArray(firstArray) ? (firstArray as T[]) : [];
  }

  private readNumber(source: unknown, ...keys: string[]): number | null {
    if (!this.isRecord(source)) return null;

    for (const key of keys) {
      const value = this.readValue(source, key);
      if (value === null || value === undefined || value === "") continue;
      const numeric = typeof value === "number" ? value : Number(value);
      if (Number.isFinite(numeric)) return numeric;
    }

    return null;
  }

  private normalizeCommandResponse<T>(
    response: ApiCommandResponse<T>,
    fallback: string,
  ): ApiCommandResponse<T> {
    const success = this.readBoolean(
      response,
      "isSuccess",
      "success",
      "succeeded",
    );
    const message =
      this.readString(response, "message", "error") ?? response.message ?? "";

    if (success === false) {
      throw new Error(message || fallback);
    }

    return {
      ...response,
      isSuccess: success ?? true,
      message,
      data: this.readValue(response, "data", "result", "value") as
        | T
        | undefined,
    };
  }

  private readBoolean(source: unknown, ...keys: string[]): boolean | null {
    if (!this.isRecord(source)) return null;

    for (const key of keys) {
      const value = this.readValue(source, key);
      if (typeof value === "boolean") return value;
      if (typeof value === "string") {
        const normalized = value.trim().toLowerCase();
        if (["true", "1", "yes"].includes(normalized)) return true;
        if (["false", "0", "no"].includes(normalized)) return false;
      }
    }

    return null;
  }

  private readString(source: unknown, ...keys: string[]): string | null {
    if (!this.isRecord(source)) return null;

    for (const key of keys) {
      const value = this.readValue(source, key);
      if (typeof value === "string" && value.trim()) return value;
    }

    return null;
  }

  private readValue(source: unknown, ...keys: string[]): unknown {
    if (!this.isRecord(source)) return undefined;

    for (const key of keys) {
      if (Object.prototype.hasOwnProperty.call(source, key)) return source[key];
    }

    const entries = Object.entries(source);
    for (const key of keys) {
      const match = entries.find(
        ([entryKey]) => entryKey.toLowerCase() === key.toLowerCase(),
      );
      if (match) return match[1];
    }

    return undefined;
  }

  private isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === "object" && value !== null;
  }

  private toUserFacingError(error: unknown, fallback: string): Error {
    if (error instanceof Error && error.message) return error;
    if (typeof error === "object" && error !== null && "error" in error) {
      const httpError = error as {
        error?: { message?: string } | string;
        message?: string;
      };
      if (typeof httpError.error === "object" && httpError.error?.message)
        return new Error(httpError.error.message);
      if (typeof httpError.error === "string" && httpError.error)
        return new Error(httpError.error);
      if (httpError.message) return new Error(httpError.message);
    }

    return new Error(fallback);
  }
}
