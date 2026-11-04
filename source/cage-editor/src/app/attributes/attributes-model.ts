export type AttributeType = 'Int' | 'String' | 'Entity' | 'EntityList';

export interface AttributeDto {
  id: string;
  name: string;
  type: AttributeType;
}

export interface AttributeRequestDto {
  name: string;
  type: AttributeType;
}
