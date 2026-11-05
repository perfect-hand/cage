import { HttpClient } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TitleVersionDto } from './title-versions-model';

export const maximumTitleVersions = 100;

@Service()
export class TitleVersionsService {
  private readonly http = inject(HttpClient);
  private readonly versionsByTitle = signal<Record<string, TitleVersionDto[]>>({});
  private readonly pendingRequests = new Map<string, Promise<TitleVersionDto[]>>();

  getVersions(titleId: string): TitleVersionDto[] {
    return this.versionsByTitle()[titleId] ?? [];
  }

  loadVersions(organizationId: string, titleId: string): Promise<TitleVersionDto[]> {
    const cachedVersions = this.getVersions(titleId);
    if (cachedVersions.length > 0) {
      return Promise.resolve(cachedVersions);
    }

    const pendingRequest = this.pendingRequests.get(titleId);
    if (pendingRequest) {
      return pendingRequest;
    }

    const request = firstValueFrom(
      this.http.get<TitleVersionDto[]>(
        `/organizations/${organizationId}/titles/${titleId}/versions`,
      ),
    )
      .then((versions) => {
        const loadedVersions = versions ?? [];
        this.setVersions(titleId, loadedVersions);
        return loadedVersions;
      })
      .finally(() => this.pendingRequests.delete(titleId));
    this.pendingRequests.set(titleId, request);
    return request;
  }

  setVersions(titleId: string, versions: TitleVersionDto[]): void {
    this.versionsByTitle.update((versionsByTitle) => ({
      ...versionsByTitle,
      [titleId]: versions,
    }));
  }
}
