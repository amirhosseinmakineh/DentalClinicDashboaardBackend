import { Injectable } from '@angular/core';

import { AuthSession, UserRole } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly tokenStorageKey = 'dental_dashboard_token';
  private readonly completedProfileStorageKey = 'dental_dashboard_profile_completed';
  private readonly profileIdStorageKey = 'dental_dashboard_profile_id';

  setToken(token: string): AuthSession {
    localStorage.removeItem(this.completedProfileStorageKey);
    localStorage.removeItem(this.profileIdStorageKey);
    localStorage.setItem(this.tokenStorageKey, token);
    return this.getSessionFromToken(token);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenStorageKey);
  }

  getSession(): AuthSession | null {
    const token = this.getToken();
    return token ? this.getSessionFromToken(token) : null;
  }

  clear(): void {
    localStorage.removeItem(this.tokenStorageKey);
    localStorage.removeItem(this.completedProfileStorageKey);
    localStorage.removeItem(this.profileIdStorageKey);
  }

  markProfileCompleted(profileId?: number): AuthSession | null {
    const session = this.getSession();

    if (!session) {
      return null;
    }

    const completedProfileId = Number(profileId);
    const hasCompletedProfileId = Number.isFinite(completedProfileId) && completedProfileId > 0;

    localStorage.setItem(this.completedProfileStorageKey, 'true');

    if (hasCompletedProfileId) {
      localStorage.setItem(this.profileIdStorageKey, `${completedProfileId}`);
    }

    return {
      ...session,
      profileId: hasCompletedProfileId ? completedProfileId : session.profileId,
      isCompleteProfile: true
    };
  }

  private getSessionFromToken(token: string): AuthSession {
    const claims = this.decodePayload(token);
    const hasCompletedProfileOverride = localStorage.getItem(this.completedProfileStorageKey) === 'true';

    return {
      token,
      userId: this.getUserId(claims),
      profileId: this.getProfileId(claims),
      role: this.getRole(claims),
      isCompleteProfile: hasCompletedProfileOverride || this.getIsCompleteProfile(claims)
    };
  }

  private decodePayload(token: string): Record<string, unknown> {
    const payload = token.split('.')[1];

    if (!payload) {
      return {};
    }

    try {
      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=');
      const json = decodeURIComponent(
        Array.from(atob(padded))
          .map((character) => `%${character.charCodeAt(0).toString(16).padStart(2, '0')}`)
          .join('')
      );
      return JSON.parse(json) as Record<string, unknown>;
    } catch {
      return {};
    }
  }

  private getUserId(claims: Record<string, unknown>): string {
    const value = this.findClaim(claims, [
      'userId',
      'UserId',
      'id',
      'Id',
      'sub',
      'nameid',
      'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
    ]);

    return `${value ?? ''}`;
  }

  private getProfileId(claims: Record<string, unknown>): number {
    const overrideValue = Number(localStorage.getItem(this.profileIdStorageKey));

    if (Number.isFinite(overrideValue) && overrideValue > 0) {
      return overrideValue;
    }

    const value = this.findClaim(claims, [
      'profileId',
      'ProfileId',
      'consultantProfileId',
      'ConsultantProfileId'
    ]);
    const numericValue = Number(value);

    return Number.isFinite(numericValue) && numericValue > 0 ? numericValue : 0;
  }

  private getRole(claims: Record<string, unknown>): UserRole {
    const roleClaim = this.findClaim(claims, [
      'role',
      'roles',
      'Role',
      'Roles',
      'roleName',
      'RoleName',
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
    ]);
    const roles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
    const normalizedRoles = roles.map((role) => `${role ?? ''}`.toLowerCase());

    if (normalizedRoles.some((role) => ['admin', 'administrator', 'مدیر', 'ادمین'].includes(role))) {
      return 'admin';
    }

    if (normalizedRoles.some((role) => ['consultant', 'advisor', 'مشاور'].includes(role))) {
      return 'consultant';
    }

    return 'unknown';
  }

  private getIsCompleteProfile(claims: Record<string, unknown>): boolean {
    const value = this.findClaim(claims, ['isCompleteProfile', 'IsCompleteProfile', 'is_complete_profile', 'IsProfileComplete', 'profileCompleted', 'ProfileCompleted']);

    if (typeof value === 'boolean') {
      return value;
    }

    return `${value ?? ''}`.toLowerCase() === 'true';
  }

  private findClaim(claims: Record<string, unknown>, keys: string[]): unknown {
    const claimKey = Object.keys(claims).find((key) => keys.includes(key));
    return claimKey ? claims[claimKey] : undefined;
  }
}
