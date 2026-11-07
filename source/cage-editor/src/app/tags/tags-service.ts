import { Service, signal } from '@angular/core';
import { TagDto } from './tags-model';

@Service()
export class TagsService {
  private readonly tagsByVersion = signal<Record<string, TagDto[]>>({});

  getTags(titleId: string, versionId: string): TagDto[] {
    return this.tagsByVersion()[`${titleId}:${versionId}`] ?? [];
  }

  hasTags(titleId: string, versionId: string): boolean {
    return Object.prototype.hasOwnProperty.call(this.tagsByVersion(), `${titleId}:${versionId}`);
  }

  setTags(titleId: string, versionId: string, tags: TagDto[]): void {
    this.tagsByVersion.update((tagsByVersion) => ({
      ...tagsByVersion,
      [`${titleId}:${versionId}`]: [...tags].sort((a, b) => a.name.localeCompare(b.name)),
    }));
  }
}
