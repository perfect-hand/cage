import { HttpClient } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TagDto } from './tags-model';

@Service()
export class TagsService {
  private readonly http = inject(HttpClient);
  private readonly tagsByVersion = signal<Record<string, TagDto[]>>({});
  private readonly pendingRequests = new Map<string, Promise<TagDto[]>>();

  getTags(titleId: string, versionId: string): TagDto[] {
    return this.tagsByVersion()[`${titleId}:${versionId}`] ?? [];
  }

  hasTags(titleId: string, versionId: string): boolean {
    return Object.prototype.hasOwnProperty.call(this.tagsByVersion(), `${titleId}:${versionId}`);
  }

  loadTags(organizationId: string, titleId: string, versionId: string): Promise<TagDto[]> {
    if (this.hasTags(titleId, versionId)) {
      return Promise.resolve(this.getTags(titleId, versionId));
    }

    const cacheKey = `${titleId}:${versionId}`;
    const pendingRequest = this.pendingRequests.get(cacheKey);
    if (pendingRequest) {
      return pendingRequest;
    }

    const request = firstValueFrom(
      this.http.get<TagDto[]>(
        `/organizations/${organizationId}/titles/${titleId}/versions/${versionId}/tags`,
      ),
    )
      .then((tags) => {
        this.setTags(titleId, versionId, tags ?? []);
        return this.getTags(titleId, versionId);
      })
      .finally(() => this.pendingRequests.delete(cacheKey));
    this.pendingRequests.set(cacheKey, request);
    return request;
  }

  setTags(titleId: string, versionId: string, tags: TagDto[]): void {
    this.tagsByVersion.update((tagsByVersion) => ({
      ...tagsByVersion,
      [`${titleId}:${versionId}`]: [...tags].sort((a, b) => a.name.localeCompare(b.name)),
    }));
  }
}
