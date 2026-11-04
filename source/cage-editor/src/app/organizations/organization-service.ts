import { Service, signal } from '@angular/core';
import { OrganizationDto } from '../api-dtos';

export type Organization = OrganizationDto;

@Service()
export class OrganizationService {
  readonly organizations = signal<Organization[]>([]);

  setOrganizations(organizations: Organization[]): void {
    this.organizations.set(organizations);
  }
}
