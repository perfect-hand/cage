import { AttributeType } from '../attributes/attributes-model';
import { TypedValueDto } from './card-types-model';

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
