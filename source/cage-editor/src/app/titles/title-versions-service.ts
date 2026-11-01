import { Service, signal } from '@angular/core';

export const maximumTitleVersions = 100;

export interface TitleVersion {
  id: string;
  name: string;
  parentVersionId: string | null;
  isReadOnly: boolean;
}

@Service()
export class TitleVersionsService {
  private readonly versionsByTitle = signal<Record<string, TitleVersion[]>>({});

  getVersions(titleId: string): TitleVersion[] {
    return this.versionsByTitle()[titleId] ?? [];
  }

  setVersions(titleId: string, versions: TitleVersion[]): void {
    this.versionsByTitle.update((versionsByTitle) => ({
      ...versionsByTitle,
      [titleId]: versions,
    }));
  }
}
