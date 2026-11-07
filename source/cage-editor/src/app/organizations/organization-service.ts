import { HttpClient } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { OrganizationDto } from './organization-model';

@Service()
export class OrganizationService {
  private readonly http = inject(HttpClient);
  readonly organizations = signal<OrganizationDto[]>([]);
  private isLoaded = false;
  private pendingRequest: Promise<OrganizationDto[]> | undefined;

  loadOrganizations(): Promise<OrganizationDto[]> {
    if (this.isLoaded) {
      return Promise.resolve(this.organizations());
    }
    if (this.pendingRequest) {
      return this.pendingRequest;
    }

    this.pendingRequest = firstValueFrom(this.http.get<OrganizationDto[]>('/organizations'))
      .then((organizations) => {
        this.setOrganizations(organizations ?? []);
        return this.organizations();
      })
      .finally(() => (this.pendingRequest = undefined));
    return this.pendingRequest;
  }

  setOrganizations(organizations: OrganizationDto[]): void {
    this.organizations.set(
      [...organizations].sort((a, b) => a.name.localeCompare(b.name)),
    );
    this.isLoaded = true;
  }
}
