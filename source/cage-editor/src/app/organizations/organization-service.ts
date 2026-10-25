import { Service, signal } from '@angular/core';

export interface Organization {
  id: string;
  name: string;
}

@Service()
export class OrganizationService {
  readonly organizations = signal<Organization[]>([]);

  setOrganizations(organizations: Organization[]): void {
    this.organizations.set(organizations);
  }
}
