import { inject } from '@angular/core';
import { CanActivateFn, RedirectCommand, Router } from '@angular/router';
import { SessionService } from './session.service';

export const adminGuard: CanActivateFn = async (_, state) => {
  const session = inject(SessionService);
  const router = inject(Router);
  const status = await session.check();
  if (status === 'authenticated') return true;
  if (status === 'guest') return router.createUrlTree(['/login']);
  return new RedirectCommand(router.createUrlTree(['/stare-sesiune']), {
    state: { status, returnTo: state.url },
  });
};
