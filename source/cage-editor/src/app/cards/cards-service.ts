import { HttpClient } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CardDto } from './cards-model';

@Service()
export class CardsService {
  private readonly http = inject(HttpClient);
  private readonly cardsByCardSet = signal<Record<string, CardDto[]>>({});
  private readonly pendingRequests = new Map<string, Promise<CardDto[]>>();
  private readonly pendingRequestTokens = new Map<string, object>();
  // Version-wide generations keep in-flight card loads from restoring stale data after invalidation.
  private readonly generationsByVersion = new Map<string, number>();

  getCards(titleId: string, versionId: string, cardSetId: string): CardDto[] {
    return this.cardsByCardSet()[this.cacheKey(titleId, versionId, cardSetId)] ?? [];
  }

  hasCards(titleId: string, versionId: string, cardSetId: string): boolean {
    return Object.prototype.hasOwnProperty.call(
      this.cardsByCardSet(),
      this.cacheKey(titleId, versionId, cardSetId),
    );
  }

  loadCards(
    organizationId: string,
    titleId: string,
    versionId: string,
    cardSetId: string,
  ): Promise<CardDto[]> {
    if (this.hasCards(titleId, versionId, cardSetId)) {
      return Promise.resolve(this.getCards(titleId, versionId, cardSetId));
    }

    const cacheKey = this.cacheKey(titleId, versionId, cardSetId);
    const pendingRequest = this.pendingRequests.get(cacheKey);
    if (pendingRequest) {
      return pendingRequest;
    }

    const generationKey = `${titleId}:${versionId}`;
    const generation = this.generationsByVersion.get(generationKey) ?? 0;
    const requestToken = {};
    const request = firstValueFrom(
      this.http.get<CardDto[]>(
        `/organizations/${organizationId}/titles/${titleId}/versions/${versionId}/card-sets/${cardSetId}/cards`,
      ),
    )
      .then((cards) => {
        if ((this.generationsByVersion.get(generationKey) ?? 0) === generation) {
          this.setCards(titleId, versionId, cardSetId, cards ?? []);
        }
        return this.getCards(titleId, versionId, cardSetId);
      })
      .finally(() => {
        if (this.pendingRequestTokens.get(cacheKey) === requestToken) {
          this.pendingRequests.delete(cacheKey);
          this.pendingRequestTokens.delete(cacheKey);
        }
      });
    this.pendingRequests.set(cacheKey, request);
    this.pendingRequestTokens.set(cacheKey, requestToken);
    return request;
  }

  setCards(titleId: string, versionId: string, cardSetId: string, cards: CardDto[]): void {
    this.cardsByCardSet.update((cardsByCardSet) => ({
      ...cardsByCardSet,
      [this.cacheKey(titleId, versionId, cardSetId)]: [...cards].sort((a, b) =>
        a.name.localeCompare(b.name),
      ),
    }));
  }

  invalidateCards(titleId: string, versionId: string, cardSetId: string): void {
    this.cardsByCardSet.update((cardsByCardSet) => {
      const updated = { ...cardsByCardSet };
      delete updated[this.cacheKey(titleId, versionId, cardSetId)];
      return updated;
    });
  }

  invalidateCardsForVersion(titleId: string, versionId: string): void {
    const cachePrefix = `${titleId}:${versionId}:`;
    const generationKey = `${titleId}:${versionId}`;
    this.generationsByVersion.set(
      generationKey,
      (this.generationsByVersion.get(generationKey) ?? 0) + 1,
    );
    this.cardsByCardSet.update((cardsByCardSet) => {
      const updated = { ...cardsByCardSet };
      for (const cacheKey of Object.keys(updated)) {
        if (cacheKey.startsWith(cachePrefix)) {
          delete updated[cacheKey];
        }
      }
      return updated;
    });
    for (const cacheKey of this.pendingRequests.keys()) {
      if (cacheKey.startsWith(cachePrefix)) {
        this.pendingRequests.delete(cacheKey);
        this.pendingRequestTokens.delete(cacheKey);
      }
    }
  }

  private cacheKey(titleId: string, versionId: string, cardSetId: string): string {
    return `${titleId}:${versionId}:${cardSetId}`;
  }
}
