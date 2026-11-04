import { HttpClient } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TitleVersionDto } from '../api-dtos';

export const maximumTitleVersions = 100;

export type TitleVersion = TitleVersionDto;

@Service()
export class TitleVersionsService {
  private readonly http = inject(HttpClient);
  private readonly versionsByTitle = signal<Record<string, TitleVersion[]>>({});
  private readonly pendingRequests = new Map<string, Promise<TitleVersion[]>>();

  getVersions(titleId: string): TitleVersion[] {
    return this.versionsByTitle()[titleId] ?? [];
  }

  loadVersions(organizationId: string, titleId: string): Promise<TitleVersion[]> {
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

  setVersions(titleId: string, versions: TitleVersion[]): void {
    this.versionsByTitle.update((versionsByTitle) => ({
      ...versionsByTitle,
      [titleId]: versions,
    }));
  }
}
