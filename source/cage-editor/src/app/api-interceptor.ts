import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, mergeMap } from 'rxjs';

import { AuthService } from './auth-service';
import { environment } from '../environments/environment';

/**
 * Resolves relative request URLs against `environment.backendUrl` and
 * attaches the current access token as an Authorization header, so
 * components can call `HttpClient` with backend-relative paths (e.g.
 * `/organizations`) without repeating the base URL or querying the
 * AuthService for a token themselves.
 *
 * Absolute URLs (e.g. requests to third-party APIs) are passed through
 * unchanged and never receive the Authorization header.
 */
export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const isAbsoluteUrl = /^[a-z][a-z\d+.-]*:\/\//i.test(req.url);

  if (isAbsoluteUrl) {
    return next(req);
  }

  const authService = inject(AuthService);
  const url = environment.backendUrl.replace(/\/$/, '') + '/' + req.url.replace(/^\//, '');

  return from(authService.getAccessToken()).pipe(
    mergeMap((token) => {
      const authorizedReq = req.clone({
        url,
        setHeaders: token
          ? {
            Authorization: 'Bearer' + ' ' + token,
          }
          : {},
      });

      return next(authorizedReq);
    }),
  );
};
