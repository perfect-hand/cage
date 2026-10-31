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
  private readonly attributesByTitle = signal<Record<string, Attribute[]>>({});

  getAttributes(titleId: string): Attribute[] {
    return this.attributesByTitle()[titleId] ?? [];
  }

  setAttributes(titleId: string, attributes: Attribute[]): void {
    this.attributesByTitle.update((attributesByTitle) => ({
      ...attributesByTitle,
      [titleId]: attributes,
    }));
  }
}
