import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../auth-service';

@Component({
  selector: 'create-organization',
  imports: [FormField, MatButtonModule, RouterLink],
  templateUrl: './create-organization.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './create-organization.scss',
})
export class CreateOrganization {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal('');
  readonly name = signal('');
  readonly nameForm = form(this.name, (path) => {
    required(path, { message: 'Enter an organization name.' });
    pattern(path, /\S/, { message: 'Enter an organization name.' });
  });

  async createOrganization(): Promise<void> {
    if (this.nameForm().invalid() || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    try {
      const token = await this.authService.getAccessToken();
      await firstValueFrom(
        this.http.post('http://localhost:5267/organizations', {
          name: this.name().trim(),
        }, {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }),
      );
      await this.router.navigateByUrl('/');
    } catch (error) {
      console.error('Failed to create organization.', error);
      this.errorMessage.set('Could not create the organization. Please try again.');
    } finally {
      this.isSubmitting.set(false);
    }
  }
}