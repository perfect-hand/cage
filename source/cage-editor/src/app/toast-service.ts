import { inject, Service } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/**
 * Re-usable way of surfacing messages (especially errors) to the user.
 */
@Service()
export class ToastService {
  private readonly snackBar = inject(MatSnackBar);

  showError(message: string): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: 8000,
      panelClass: 'error-toast',
    });
  }
}
