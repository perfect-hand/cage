import { Service, signal } from '@angular/core';
import { OrganizationDto } from './organization-model';

@Service()
export class OrganizationService {
  readonly organizations = signal<OrganizationDto[]>([]);

  setOrganizations(organizations: OrganizationDto[]): void {
    this.organizations.set(
      [...organizations].sort((a, b) => a.name.localeCompare(b.name)),
    );
  }
}
