import { Service, signal } from '@angular/core';
import { TitleDto } from '../api-dtos';

export type Title = TitleDto;

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
