import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface ConfirmDeleteDialogData {
  name: string;
  consequence?: string;
  requireNameConfirmation?: boolean;
}

@Component({
  selector: 'confirm-delete-dialog',
  imports: [MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './confirm-delete-dialog.html',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class ConfirmDeleteDialog {
  readonly data = inject<ConfirmDeleteDialogData>(MAT_DIALOG_DATA);
  readonly typedName = signal('');
}
