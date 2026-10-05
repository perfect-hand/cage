import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { ToastService } from './toast-service';

function describeHttpError(error: HttpErrorResponse): string {
  switch (error.status) {
    case 0:
      return 'The server could not be reached. Please try again later.';
    case 401:
      return 'Authentication failed. Please sign in again.';
    case 403:
      return 'You are not allowed to perform this action.';
    default:
      return error.status >= 500
        ? 'The server ran into an error. Please try again later.'
        : 'The request failed (' + error.status + ').';
  }
}

/**
 * Shows a toast for every failed HTTP request and rethrows the error so
 * callers can still handle it.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toastService = inject(ToastService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        toastService.showError(describeHttpError(error));
      }

      return throwError(() => error);
    }),
  );
};
