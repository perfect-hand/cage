import { HttpClient } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export const maximumTitleVersions = 100;

export interface TitleVersion {
  id: string;
  name: string;
  parentVersionId: string | null;
  isReadOnly: boolean;
}

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
      this.http.get<TitleVersion[]>(
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
