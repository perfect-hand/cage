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

export interface OrganizationDto {
  id: string;
  name: string;
}

export interface OrganizationRequestDto {
  name: string;
}

export interface TitleDto {
  id: string;
  name: string;
}

export interface TitleRequestDto {
  name: string;
}

export interface TitleVersionDto {
  id: string;
  name: string;
  parentVersionId: string | null;
  isReadOnly: boolean;
}

export interface CreateTitleVersionRequestDto {
  name: string;
  parentVersionId: string | null;
}

export interface RenameTitleVersionRequestDto {
  name: string;
}

export interface SetTitleVersionReadOnlyRequestDto {
  isReadOnly: boolean;
}
