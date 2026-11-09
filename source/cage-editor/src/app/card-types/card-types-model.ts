import { AttributeType } from '../attributes/attributes-model';

export interface EntityValueDto {
  id: number;
  attributes: Record<string, TypedValueDto>;
  tags: string[];
}

export interface TypedValueDto {
  type: AttributeType;
  value: number | string | EntityValueDto | EntityValueDto[];
}

export interface CardTypeDto {
  id: string;
  name: string;
  parentId: string | null;
  attributes: Record<string, TypedValueDto>;
  tags: string[];
}

export interface CardTypeRequestDto {
  name: string;
  parentId: string | null;
  attributes: Record<string, TypedValueDto>;
  tags: string[];
}
