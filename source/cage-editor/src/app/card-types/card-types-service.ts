import { HttpClient } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CardTypeDto } from './card-types-model';

@Service()
export class CardTypesService {
  private readonly http = inject(HttpClient);
  private readonly cardTypesByVersion = signal<Record<string, CardTypeDto[]>>({});
  private readonly pendingRequests = new Map<string, Promise<CardTypeDto[]>>();

  getCardTypes(titleId: string, versionId: string): CardTypeDto[] {
    return this.cardTypesByVersion()[`${titleId}:${versionId}`] ?? [];
  }

  hasCardTypes(titleId: string, versionId: string): boolean {
    return Object.prototype.hasOwnProperty.call(
      this.cardTypesByVersion(),
      `${titleId}:${versionId}`,
    );
  }

  loadCardTypes(
    organizationId: string,
    titleId: string,
    versionId: string,
  ): Promise<CardTypeDto[]> {
    if (this.hasCardTypes(titleId, versionId)) {
      return Promise.resolve(this.getCardTypes(titleId, versionId));
    }

    const cacheKey = `${titleId}:${versionId}`;
    const pendingRequest = this.pendingRequests.get(cacheKey);
    if (pendingRequest) {
      return pendingRequest;
    }

    const request = firstValueFrom(
      this.http.get<CardTypeDto[]>(
        `/organizations/${organizationId}/titles/${titleId}/versions/${versionId}/card-types`,
      ),
    )
      .then((cardTypes) => {
        this.setCardTypes(titleId, versionId, cardTypes ?? []);
        return this.getCardTypes(titleId, versionId);
      })
      .finally(() => this.pendingRequests.delete(cacheKey));
    this.pendingRequests.set(cacheKey, request);
    return request;
  }

  setCardTypes(titleId: string, versionId: string, cardTypes: CardTypeDto[]): void {
    this.cardTypesByVersion.update((cardTypesByVersion) => ({
      ...cardTypesByVersion,
      [`${titleId}:${versionId}`]: [...cardTypes].sort((a, b) => a.name.localeCompare(b.name)),
    }));
  }
}
