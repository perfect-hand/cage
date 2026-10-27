import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Organization, OrganizationService } from './organization-service';

@Component({
  selector: 'edit-organization',
  imports: [FormField, MatButtonModule, MatProgressSpinnerModule, RouterLink],
  templateUrl: './edit-organization.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './edit-organization.scss',
})
export class EditOrganization implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly organizationService = inject(OrganizationService);

  readonly organizationId = input.required<string>();
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly notFound = signal(false);
  readonly name = signal('');
  readonly nameForm = form(this.name, (path) => {
    required(path, { message: 'Enter an organization name.' });
    pattern(path, /\S/, { message: 'Enter an organization name.' });
    maxLength(path, 50, { message: 'Enter an organization name of at most 50 characters.' });
  });

  ngOnInit(): void {
    void this.loadOrganization();
  }

  private async loadOrganization(): Promise<void> {
    let organization = this.organizationService
      .organizations()
      .find((item) => item.id === this.organizationId());

    if (!organization) {
      try {
        const organizations = await firstValueFrom(this.http.get<Organization[]>('/organizations'));
        this.organizationService.setOrganizations(organizations ?? []);
        organization = organizations?.find((item) => item.id === this.organizationId());
      } catch (error) {
        console.error('Failed to load organizations.', error);
      }
    }

    if (organization) {
      this.name.set(organization.name);
    } else {
      this.notFound.set(true);
    }
    this.isLoading.set(false);
  }

  async updateOrganization(): Promise<void> {
    if (this.nameForm().invalid() || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);

    try {
      const name = this.name().trim();
      await firstValueFrom(
        this.http.put(`/organizations/${this.organizationId()}`, { name }),
      );
      this.organizationService.setOrganizations(
        this.organizationService.organizations().map((organization) =>
          organization.id === this.organizationId() ? { ...organization, name } : organization,
        ),
      );
      await this.router.navigateByUrl('/');
    } catch (error) {
      console.error('Failed to update organization.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
