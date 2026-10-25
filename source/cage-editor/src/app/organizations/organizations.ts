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
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Organization, OrganizationService } from './organization-service';

@Component({
  selector: 'organizations',
  imports: [MatProgressSpinnerModule, MatButtonModule, RouterLink],
  templateUrl: './organizations.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './organizations.scss',
})
export class Organizations implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly organizationService = inject(OrganizationService);

  readonly organizations = this.organizationService.organizations;
  readonly isLoading = signal(false);

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
}
