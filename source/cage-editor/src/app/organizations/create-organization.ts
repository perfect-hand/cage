import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'create-organization',
  imports: [FormField, MatButtonModule, RouterLink],
  templateUrl: './create-organization.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './create-organization.scss',
})
export class CreateOrganization {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly isSubmitting = signal(false);
  readonly atLimit = signal(false);
  readonly name = signal('');
  readonly nameForm = form(this.name, (path) => {
    required(path, { message: 'Enter an organization name.' });
    pattern(path, /\S/, { message: 'Enter an organization name.' });
    maxLength(path, 50, { message: 'Enter an organization name of at most 50 characters.' });
  });

  async createOrganization(): Promise<void> {
    if (this.nameForm().invalid() || this.isSubmitting() || this.atLimit()) {
      return;
    }

    this.isSubmitting.set(true);

    try {
      const organizations = await firstValueFrom(
        this.http.get<{ id: string; name: string }[]>('/organizations'),
      );
      if (organizations.length >= 10) {
        this.atLimit.set(true);
        return;
      }

      await firstValueFrom(
        this.http.post('/organizations', {
          name: this.name().trim(),
        }),
      );
      await this.router.navigateByUrl('/');
    } catch (error) {
      console.error('Failed to create organization.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
