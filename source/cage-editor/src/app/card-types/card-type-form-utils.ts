import { AttributeType } from '../attributes/attributes-model';
import { EntityValueDto, TypedValueDto } from './card-types-model';

export function defaultAttributeValue(type: AttributeType): string {
  switch (type) {
    case 'Int':
      return '0';
    case 'String':
      return '';
    case 'Entity':
      return '{"id":0,"attributes":{},"tags":[]}';
    case 'EntityList':
      return '[]';
  }
}

export function formatAttributeValue(value: TypedValueDto): string {
  return typeof value.value === 'string' || typeof value.value === 'number'
    ? String(value.value)
    : JSON.stringify(value.value);
}

export function parseAttributeValue(type: AttributeType, value: string): TypedValueDto {
  if (type === 'Int') {
    const parsedValue = Number(value);
    if (!Number.isInteger(parsedValue)) {
      throw new Error('Enter a whole number.');
    }
    return { type, value: parsedValue };
  }

  if (type === 'String') {
    return { type, value };
  }

  let parsedValue: unknown;
  try {
    parsedValue = JSON.parse(value) as unknown;
  } catch {
    throw new Error('Enter valid JSON.');
  }

  if (type === 'Entity') {
    if (!isEntityValue(parsedValue)) {
      throw new Error('Enter a JSON entity with an id, attributes, and tags.');
    }
    return { type, value: parsedValue };
  }

  if (!Array.isArray(parsedValue) || !parsedValue.every(isEntityValue)) {
    throw new Error('Enter a JSON array of entities.');
  }
  return { type, value: parsedValue };
}

function isEntityValue(value: unknown): value is EntityValueDto {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    return false;
  }
  const entity = value as Record<string, unknown>;
  return (
    typeof entity['id'] === 'number' &&
    typeof entity['attributes'] === 'object' &&
    entity['attributes'] !== null &&
    !Array.isArray(entity['attributes']) &&
    Array.isArray(entity['tags']) &&
    entity['tags'].every((tag) => typeof tag === 'string')
  );
}
