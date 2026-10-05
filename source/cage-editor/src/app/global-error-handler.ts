import { ErrorHandler, inject, Service } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

import { ToastService } from './toast-service';

/**
 * Logs unhandled errors and surfaces them to the user. HTTP errors are
 * already surfaced by the error interceptor.
 */
@Service()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly toastService = inject(ToastService);

  handleError(error: unknown): void {
    console.error(error);

    if (!(error instanceof HttpErrorResponse)) {
      this.toastService.showError('An unexpected error occurred.');
    }
  }
}
