import { HttpClient } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TitleDto } from './titles-model';

@Service()
export class TitlesService {
  private readonly http = inject(HttpClient);
  private readonly titlesByOrganization = signal<Record<string, TitleDto[]>>({});
  private readonly pendingRequests = new Map<string, Promise<TitleDto[]>>();

  getTitles(organizationId: string): TitleDto[] {
    return this.titlesByOrganization()[organizationId] ?? [];
  }

  loadTitles(organizationId: string): Promise<TitleDto[]> {
    if (Object.prototype.hasOwnProperty.call(this.titlesByOrganization(), organizationId)) {
      return Promise.resolve(this.getTitles(organizationId));
    }
    const pendingRequest = this.pendingRequests.get(organizationId);
    if (pendingRequest) {
      return pendingRequest;
    }

    const request = firstValueFrom(
      this.http.get<TitleDto[]>(`/organizations/${organizationId}/titles`),
    )
      .then((titles) => {
        this.setTitles(organizationId, titles ?? []);
        return this.getTitles(organizationId);
      })
      .finally(() => this.pendingRequests.delete(organizationId));
    this.pendingRequests.set(organizationId, request);
    return request;
  }

  setTitles(organizationId: string, titles: TitleDto[]): void {
    this.titlesByOrganization.update((titlesByOrganization) => ({
      ...titlesByOrganization,
      [organizationId]: [...titles].sort((a, b) => a.name.localeCompare(b.name)),
    }));
  }
}
