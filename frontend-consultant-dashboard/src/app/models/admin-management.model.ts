import { Gender } from './register-command.model';

export interface AdminUser {
  id?: number | string | null;
  userId?: number | string | null;
  firstName?: string | null;
  lastName?: string | null;
  fullName?: string | null;
  name?: string | null;
  phoneNumber?: string | null;
  phone?: string | null;
  role?: string | null;
  roleName?: string | null;
  status?: string | null;
  isActive?: boolean | null;
  isCompleteProfile?: boolean | null;
  avatarImageName?: string | null;
  gender?: Gender | number | null;
  birthDate?: string | null;
}

export interface AdminRole {
  id?: number | string | null;
  roleId?: number | string | null;
  roleName?: string | null;
}

export interface CreateUserCommandPayload {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  passwordHash: string;
  birthDate: string;
  gender: Gender;
  avatarImageName?: string | null;
  roleName: string;
}

export interface UpdateUserCommandPayload {
  id: number | string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  isCompleteProfile: boolean;
  avatarImageName?: string | null;
  gender: Gender;
  isActive: boolean;
  roleName: string;
}

export interface DeleteUserCommandPayload {
  userId: number | string;
}

export type UserCommandPayload = CreateUserCommandPayload | UpdateUserCommandPayload | DeleteUserCommandPayload;

export interface RoleCommandPayload {
  id?: number | string;
  roleName: string;
}
