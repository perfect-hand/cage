import { Service, signal } from '@angular/core';

export type AttributeType = 'Int' | 'String' | 'Entity' | 'EntityList';

export const attributeTypes: { value: AttributeType; label: string }[] = [
  { value: 'Int', label: 'Int' },
  { value: 'String', label: 'String' },
  { value: 'Entity', label: 'Entity' },
  { value: 'EntityList', label: 'Entity List' },
];

export interface Attribute {
  id: string;
  name: string;
  type: AttributeType;
}

@Service()
export class AttributesService {
  private readonly attributesByVersion = signal<Record<string, Attribute[]>>({});

  getAttributes(titleId: string, versionId: string): Attribute[] {
    return this.attributesByVersion()[`${titleId}:${versionId}`] ?? [];
  }

  setAttributes(titleId: string, versionId: string, attributes: Attribute[]): void {
    this.attributesByVersion.update((attributesByVersion) => ({
      ...attributesByVersion,
      [`${titleId}:${versionId}`]: attributes,
    }));
  }
}
