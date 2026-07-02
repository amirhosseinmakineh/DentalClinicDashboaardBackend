import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthSessionService } from '../services/auth-session.service';

export const consultantProfileGuard: CanActivateFn = () => {
  const authSession = inject(AuthSessionService);
  const router = inject(Router);
  const session = authSession.getSession();

  if (!session) {
    return router.createUrlTree(['/login']);
  }

  if (session.role !== 'consultant' || session.isCompleteProfile) {
    return true;
  }

  return router.createUrlTree(['/consultant/complete-profile']);
};

export const incompleteConsultantProfileGuard: CanActivateFn = () => {
  const authSession = inject(AuthSessionService);
  const router = inject(Router);
  const session = authSession.getSession();

  if (!session) {
    return router.createUrlTree(['/login']);
  }

  if (session.role === 'consultant' && !session.isCompleteProfile) {
    return true;
  }

  return router.createUrlTree([session.role === 'admin' ? '/admin' : '/consultant/dashboard']);
};
