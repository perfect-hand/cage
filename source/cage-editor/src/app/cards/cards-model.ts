import { TypedValueDto } from '../card-types/card-types-model';

export interface CardDto {
  id: string;
  name: string;
  cardTypeId: string;
  attributes: Record<string, TypedValueDto>;
  tags: string[];
}

export interface CardRequestDto {
  name: string;
  cardTypeId: string;
  attributes: Record<string, TypedValueDto>;
  tags: string[];
}
