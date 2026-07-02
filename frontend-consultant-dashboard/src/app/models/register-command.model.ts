export enum Gender {
  Male = 1,
  Female = 2
}

export interface RegisterCommand {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  passwordHash: string;
  isCompleteProfile: boolean;
  avatarImageName?: string | null;
  gender: Gender;
  birthDate: string;
}
