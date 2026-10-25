import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { form, FormField, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { OrganizationBreadcrumb } from '../organizations/organization-breadcrumb';

@Component({
  selector: 'create-title',
  imports: [FormField, MatButtonModule, RouterLink, OrganizationBreadcrumb],
  templateUrl: './create-title.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './create-title.scss',
})
export class CreateTitle {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly organizationId = input.required<string>();
  readonly isSubmitting = signal(false);
  readonly name = signal('');
  readonly nameForm = form(this.name, (path) => {
    required(path, { message: 'Enter a title name.' });
    pattern(path, /\S/, { message: 'Enter a title name.' });
  });

  async createTitle(): Promise<void> {
    if (this.nameForm().invalid() || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);

    try {
      await firstValueFrom(
        this.http.post(`/organizations/${this.organizationId()}/titles`, {
          name: this.name().trim(),
        }),
      );
      await this.router.navigate(['/organizations', this.organizationId(), 'titles']);
    } catch (error) {
      console.error('Failed to create title.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
