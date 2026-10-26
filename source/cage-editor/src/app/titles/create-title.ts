import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
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
  readonly atLimit = signal(false);
  readonly name = signal('');
  readonly nameForm = form(this.name, (path) => {
    required(path, { message: 'Enter a title name.' });
    pattern(path, /\S/, { message: 'Enter a title name.' });
    maxLength(path, 50, { message: 'Enter a title name of at most 50 characters.' });
  });

  async createTitle(): Promise<void> {
    if (this.nameForm().invalid() || this.isSubmitting() || this.atLimit()) {
      return;
    }

    this.isSubmitting.set(true);

    try {
      const titles = await firstValueFrom(
        this.http.get<{ id: string; name: string }[]>(
          `/organizations/${this.organizationId()}/titles`,
        ),
      );
      if (titles.length >= 100) {
        this.atLimit.set(true);
        return;
      }

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
