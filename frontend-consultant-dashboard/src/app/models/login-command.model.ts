export interface LoginCommand {
  userId?: string;
  phoneNumber: string;
  passwordHash: string;
}
