export interface TitleVersionDto {
  id: string;
  name: string;
  parentVersionId: string | null;
  isReadOnly: boolean;
  createdAt: string;
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
