import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ConfirmDeleteDialog } from '../confirm-delete-dialog';
import { Organization, OrganizationService } from './organization-service';

@Component({
  selector: 'organizations',
  imports: [MatProgressSpinnerModule, MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './organizations.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './organizations.scss',
})
export class Organizations implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly organizationService = inject(OrganizationService);

  readonly organizations = this.organizationService.organizations;
  readonly isLoading = signal(false);
  readonly isDeleting = signal(false);

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

  async deleteOrganization(organizationId: string, name: string): Promise<void> {
    if (this.isDeleting()) {
      return;
    }

    const confirmed = await firstValueFrom(
      this.dialog
        .open(ConfirmDeleteDialog, {
          data: {
            name,
            requireNameConfirmation: true,
            consequence: 'All titles of that organization will be deleted as well.',
          },
        })
        .afterClosed(),
    );

    if (!confirmed) {
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
