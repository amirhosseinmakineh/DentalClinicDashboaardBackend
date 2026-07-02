export interface CreateReservationCommand {
  leadAssignmentId: number;
  consultantProfileId: number;
  reservationAt: string;
  description: string;
}

export interface CreateReservationResponse {
  id: number;
  leadAssignmentId: number;
  consultantProfileId: number;
  reservationAt: string;
  patientName?: string;
  patientPhoneNumber?: string;
}

export interface CreateReservationApiResult {
  isSuccess?: boolean;
  IsSuccess?: boolean;
  message?: string;
  Message?: string;
  data?: Partial<CreateReservationResponse> | null;
  Data?: Partial<CreateReservationResponse> | null;
}
