import { Service, signal } from '@angular/core';
import { AttributeDto, AttributeType } from './attributes-model';

export const attributeTypes: { value: AttributeType; label: string }[] = [
  { value: 'Int', label: 'Int' },
  { value: 'String', label: 'String' },
  { value: 'Entity', label: 'Entity' },
  { value: 'EntityList', label: 'Entity List' },
];

@Service()
export class AttributesService {
  private readonly attributesByVersion = signal<Record<string, AttributeDto[]>>({});

  getAttributes(titleId: string, versionId: string): AttributeDto[] {
    return this.attributesByVersion()[`${titleId}:${versionId}`] ?? [];
  }

  setAttributes(titleId: string, versionId: string, attributes: AttributeDto[]): void {
    this.attributesByVersion.update((attributesByVersion) => ({
      ...attributesByVersion,
      [`${titleId}:${versionId}`]: [...attributes].sort((a, b) => a.name.localeCompare(b.name)),
    }));
  }
}
