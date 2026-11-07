import { HttpClient } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AttributeDto, AttributeType } from './attributes-model';

export const attributeTypes: { value: AttributeType; label: string }[] = [
  { value: 'Int', label: 'Int' },
  { value: 'String', label: 'String' },
  { value: 'Entity', label: 'Entity' },
  { value: 'EntityList', label: 'Entity List' },
];

@Service()
export class AttributesService {
  private readonly http = inject(HttpClient);
  private readonly attributesByVersion = signal<Record<string, AttributeDto[]>>({});
  private readonly pendingRequests = new Map<string, Promise<AttributeDto[]>>();

  getAttributes(titleId: string, versionId: string): AttributeDto[] {
    return this.attributesByVersion()[`${titleId}:${versionId}`] ?? [];
  }

  hasAttributes(titleId: string, versionId: string): boolean {
    return Object.prototype.hasOwnProperty.call(
      this.attributesByVersion(),
      `${titleId}:${versionId}`,
    );
  }

  loadAttributes(
    organizationId: string,
    titleId: string,
    versionId: string,
  ): Promise<AttributeDto[]> {
    if (this.hasAttributes(titleId, versionId)) {
      return Promise.resolve(this.getAttributes(titleId, versionId));
    }

    const cacheKey = `${titleId}:${versionId}`;
    const pendingRequest = this.pendingRequests.get(cacheKey);
    if (pendingRequest) {
      return pendingRequest;
    }

    const request = firstValueFrom(
      this.http.get<AttributeDto[]>(
        `/organizations/${organizationId}/titles/${titleId}/versions/${versionId}/attributes`,
      ),
    )
      .then((attributes) => {
        this.setAttributes(titleId, versionId, attributes ?? []);
        return this.getAttributes(titleId, versionId);
      })
      .finally(() => this.pendingRequests.delete(cacheKey));
    this.pendingRequests.set(cacheKey, request);
    return request;
  }

  setAttributes(titleId: string, versionId: string, attributes: AttributeDto[]): void {
    this.attributesByVersion.update((attributesByVersion) => ({
      ...attributesByVersion,
      [`${titleId}:${versionId}`]: [...attributes].sort((a, b) => a.name.localeCompare(b.name)),
    }));
  }
}
