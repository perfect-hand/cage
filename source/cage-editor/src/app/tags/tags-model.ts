export interface TagDto {
  id: string;
  name: string;
  referenceCount?: number;
}

export interface TagRequestDto {
  name: string;
}
