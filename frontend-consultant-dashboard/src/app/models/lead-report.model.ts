export type LeadCallResult =
  | 'Contacted'
  | 'Converted'
  | 'Rejected'
  | 'NoAnswer'
  | 'WrongNumber'
  | 'NeedFollowUp'
  | number;

export interface SubmitLeadCallReportCommand {
  leadAssignmentId: number;
  consultantProfileId: number;
  callResult: LeadCallResult;
  reportDescription: string;
  patientCity?: string;
  patientRegion?: string;
  businessName?: string;
  attendanceProbabilityPercent?: number;
  secondaryPhoneNumber?: string;
}

export interface SubmitLeadCallReportResponse {
  leadAssignmentId: number;
  consultantProfileId: number;
  isReportSubmitted: boolean;
  reportSubmittedAt: string;
  leadAssignmentState: string | number;
  callResult: LeadCallResult;
  isConsultantOnline: boolean;
  shouldOpenReservationPage: boolean;
  canCreateReservation: boolean;
  autoOnlineApplied: boolean;
  autoOnlineBlockedReason?: string | null;
}

export interface SubmitLeadCallReportApiResult {
  isSuccess?: boolean;
  IsSuccess?: boolean;
  message?: string;
  Message?: string;
  data?: Partial<SubmitLeadCallReportResponse> | null;
  Data?: Partial<SubmitLeadCallReportResponse> | null;
}

export const LEAD_CALL_RESULT_OPTIONS: ReadonlyArray<{ value: LeadCallResult; label: string }> = [
  { value: 1, label: 'تماس موفق' },
  { value: 2, label: 'تبدیل به بیمار' },
  { value: 3, label: 'رد شد' },
  { value: 4, label: 'پاسخ نداد' },
  { value: 5, label: 'شماره اشتباه' },
  { value: 6, label: 'نیاز به پیگیری' }
];
