import { AttributeType } from '../attributes/attributes-model';
import { CardTypeDto, TypedValueDto } from './card-types-model';

export type EditableAttributeType = Extract<AttributeType, 'Int' | 'String'>;

export function isEditableAttributeType(type: AttributeType): type is EditableAttributeType {
  return type === 'Int' || type === 'String';
}

export function defaultAttributeValue(type: EditableAttributeType): string {
  return type === 'Int' ? '0' : '';
}

export function formatAttributeValue(value: TypedValueDto): string {
  return String(value.value);
}

export function parseAttributeValue(type: EditableAttributeType, value: string): TypedValueDto {
  if (type === 'Int') {
    const parsedValue = Number(value);
    if (!Number.isInteger(parsedValue)) {
      throw new Error('Enter a whole number.');
    }
    return { type, value: parsedValue };
  }

  return { type, value };
}

export function inheritedCardTypeValues(
  parentId: string,
  cardTypes: CardTypeDto[],
): {
  attributes: Map<string, TypedValueDto>;
  tags: Set<string>;
} {
  const attributes = new Map<string, TypedValueDto>();
  const tags = new Set<string>();
  const visited = new Set<string>();
  while (parentId && !visited.has(parentId)) {
    visited.add(parentId);
    const parent = cardTypes.find((cardType) => cardType.id === parentId);
    if (!parent) break;
    for (const [name, value] of Object.entries(parent.attributes)) {
      if (!attributes.has(name)) attributes.set(name, value);
    }
    parent.tags.forEach((tag) => tags.add(tag));
    parentId = parent.parentId ?? '';
  }
  return { attributes, tags };
}
