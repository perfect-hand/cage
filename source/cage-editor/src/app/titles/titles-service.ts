import { Service, signal } from '@angular/core';

export interface Title {
  id: string;
  name: string;
}

@Service()
export class TitlesService {
  private readonly titlesByOrganization = signal<Record<string, Title[]>>({});

  getTitles(organizationId: string): Title[] {
    return this.titlesByOrganization()[organizationId] ?? [];
  }

  setTitles(organizationId: string, titles: Title[]): void {
    this.titlesByOrganization.update((titlesByOrganization) => ({
      ...titlesByOrganization,
      [organizationId]: titles,
    }));
  }
}
