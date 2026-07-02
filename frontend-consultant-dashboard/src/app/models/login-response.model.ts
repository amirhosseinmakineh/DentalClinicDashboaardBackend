export interface LoginResponse {
  userId: string;
  firstName?: string;
  lastName?: string;
  phoneNumber: string;
  roles: string[];
  token: string;
}
