export type LeadType = 'RealTime' | 'OfflineQueue';
export type LeadStatus = 'Pending' | 'Assigned' | 'Expired';

export interface ConsultantProfile {
  id: string;
  fullName: string;
  phoneNumber: string;
  isActive: boolean;
  isPresent: boolean;
  isOnline: boolean;
  profileCompletionPercent: number;
}

export interface LeadAssignment {
  id: string;
  leadName: string;
  phoneNumber: string;
  type: LeadType;
  status: LeadStatus;
  createdAt: string;
  assignedAt: string;
  expiresAt?: string;
  source: string;
  note: string;
}

export interface ConsultantNotification {
  id: string;
  title: string;
  message: string;
  createdAt: string;
  isRead: boolean;
  leadId?: string;
}
