import { LeadCallResult } from './lead-report.model';

export type LeadAssignmentType = 'RealTime' | 'OfflineQueue' | number;
export type LeadAssignmentState =
  | 'New'
  | 'Assigned'
  | 'Pending'
  | 'Contacted'
  | 'Converted'
  | 'Expired'
  | 'Rejected'
  | number;

export type LeadFilter =
  | 'all'
  | 'new'
  | 'offlineQueue'
  | 'pending'
  | 'today'
  | 'approved'
  | 'expired';

export interface LeadAssignmentItem {
  id: number;
  userName: string;
  phoneNumber: string;
  leadAssignmentState: LeadAssignmentState;
  leadAssignmentType: LeadAssignmentType;
  reportSubmittedAt?: string | null;
  callResult?: LeadCallResult | null;
  isReportSubmitted: boolean;
}

export interface GetLeadsQueryParams {
  ProfileId: number;
  leadAssignmentState?: LeadAssignmentState;
  LeadAssignmentType?: 'OfflineQueue';
  PageNumber: number;
  PageSize: number;
}
