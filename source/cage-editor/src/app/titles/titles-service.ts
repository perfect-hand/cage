import { Service, signal } from '@angular/core';
import { TitleDto } from './title-dto';

@Service()
export class TitlesService {
  private readonly titlesByOrganization = signal<Record<string, TitleDto[]>>({});

  getTitles(organizationId: string): TitleDto[] {
    return this.titlesByOrganization()[organizationId] ?? [];
  }

  setTitles(organizationId: string, titles: TitleDto[]): void {
    this.titlesByOrganization.update((titlesByOrganization) => ({
      ...titlesByOrganization,
      [organizationId]: titles,
    }));
  }
}
