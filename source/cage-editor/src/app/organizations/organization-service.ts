import { Service, signal } from '@angular/core';
import { OrganizationDto } from './organization-dto';

@Service()
export class OrganizationService {
  readonly organizations = signal<OrganizationDto[]>([]);

  setOrganizations(organizations: OrganizationDto[]): void {
    this.organizations.set(organizations);
  }
}
