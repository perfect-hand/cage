import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Organization, OrganizationService } from './organization-service';

@Component({
  selector: 'organizations',
  imports: [FormField, MatProgressSpinnerModule, MatButtonModule, RouterLink],
  templateUrl: './organizations.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './organizations.scss',
})
export class Organizations implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly organizationService = inject(OrganizationService);

  readonly organizations = this.organizationService.organizations;
  readonly isLoading = signal(false);
  readonly editingOrganizationId = signal<string | null>(null);
  readonly isSaving = signal(false);
  readonly isDeleting = signal(false);
  readonly name = signal('');
  readonly nameForm = form(this.name, (path) => {
    required(path, { message: 'Enter an organization name.' });
    pattern(path, /\S/, { message: 'Enter an organization name.' });
    maxLength(path, 50, { message: 'Enter an organization name of at most 50 characters.' });
  });

  ngOnInit(): void {
    void this.loadOrganizations();
  }

  private async loadOrganizations(): Promise<void> {
    this.isLoading.set(true);

    try {
      const response = await firstValueFrom(
        this.http.get<Organization[]>('/organizations'),
      );

      this.organizationService.setOrganizations(response ?? []);
    } catch (error) {
      console.error('Failed to load organizations.', error);
      this.organizationService.setOrganizations([]);
    } finally {
      this.isLoading.set(false);
    }
  }

  startEditing(organizationId: string, name: string): void {
    this.name.set(name);
    this.editingOrganizationId.set(organizationId);
  }

  cancelEditing(): void {
    this.editingOrganizationId.set(null);
  }

  async updateOrganization(organizationId: string): Promise<void> {
    if (this.nameForm().invalid() || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);

    try {
      const name = this.name().trim();
      await firstValueFrom(
        this.http.put(`/organizations/${organizationId}`, { name }),
      );
      this.organizationService.setOrganizations(
        this.organizations().map((organization) =>
          organization.id === organizationId ? { ...organization, name } : organization,
        ),
      );
      this.editingOrganizationId.set(null);
    } catch (error) {
      console.error('Failed to update organization.', error);
    } finally {
      this.isSaving.set(false);
    }
  }

  async deleteOrganization(organizationId: string, name: string): Promise<void> {
    if (this.isDeleting() || !window.confirm(`Delete "${name}" and all its titles?`)) {
      return;
    }

    this.isDeleting.set(true);

    try {
      await firstValueFrom(this.http.delete(`/organizations/${organizationId}`));
      this.organizationService.setOrganizations(
        this.organizations().filter((organization) => organization.id !== organizationId),
      );
    } catch (error) {
      console.error('Failed to delete organization.', error);
    } finally {
      this.isDeleting.set(false);
    }
  }
}
