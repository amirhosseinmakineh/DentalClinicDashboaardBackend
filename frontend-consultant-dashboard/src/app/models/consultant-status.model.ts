export interface SetAvailableCommand {
  profileId: number;
  isAvailable: boolean;
}

export interface SetAvailableApiCommand {
  ProfileId: number;
  IsAvailable: boolean;
}

export interface SetOnlineOfflineCommand {
  profileId: number;
  isOnline: boolean;
}

export interface SetOnlineOfflineApiCommand {
  ProfileId: number;
  IsOnline: boolean;
}

export interface ConsultantStatusSnapshot {
  profileId: number;
  isAvailable: boolean;
  isOnline: boolean;
  pendingOfflineLeadCount: number;
  currentScore: number;
  canGoOnline: boolean;
  onlineStatusBlockReason?: string | null;
}

export interface ConsultantStatusState extends ConsultantStatusSnapshot {
  isLoading: boolean;
  isSubmittingAvailable: boolean;
  isSubmittingOnline: boolean;
}

export type ConsultantStatusApiData = Partial<ConsultantStatusSnapshot> & {
  ProfileId?: number;
  IsAvailable?: boolean;
  IsOnline?: boolean;
  PendingOfflineLeadCount?: number;
  CurrentScore?: number;
  CanGoOnline?: boolean;
  OnlineStatusBlockReason?: string | null;
};

export interface ConsultantStatusApiResult {
  isSuccess?: boolean;
  IsSuccess?: boolean;
  message?: string;
  Message?: string;
  data?: ConsultantStatusApiData | null;
  Data?: ConsultantStatusApiData | null;
}
