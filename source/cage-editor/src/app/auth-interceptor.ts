import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, mergeMap } from 'rxjs';

import { AuthService } from './auth-service';
import { environment } from '../environments/environment';

/**
 * Attaches the current access token as a Bearer Authorization header to
 * outgoing requests targeting the Cage backend, so components no longer
 * need to query the AuthService for a token themselves.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.backendUrl)) {
    return next(req);
  }

  const authService = inject(AuthService);

  return from(authService.getAccessToken()).pipe(
    mergeMap((token) => {
      const authorizedReq = token
        ? req.clone({
            setHeaders: {
              Authorization: `Bearer ${token}`,
            },
          })
        : req;

      return next(authorizedReq);
    }),
  );
};
