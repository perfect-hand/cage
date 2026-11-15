import { HttpClient } from '@angular/common/http';
import { inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CardSetDto } from './card-sets-model';

@Service()
export class CardSetsService {
  private readonly http = inject(HttpClient);
  private readonly cardSetsByVersion = signal<Record<string, CardSetDto[]>>({});
  private readonly pendingRequests = new Map<string, Promise<CardSetDto[]>>();

  getCardSets(titleId: string, versionId: string): CardSetDto[] {
    return this.cardSetsByVersion()[`${titleId}:${versionId}`] ?? [];
  }

  hasCardSets(titleId: string, versionId: string): boolean {
    return Object.prototype.hasOwnProperty.call(
      this.cardSetsByVersion(),
      `${titleId}:${versionId}`,
    );
  }

  loadCardSets(organizationId: string, titleId: string, versionId: string): Promise<CardSetDto[]> {
    if (this.hasCardSets(titleId, versionId)) {
      return Promise.resolve(this.getCardSets(titleId, versionId));
    }

    const cacheKey = `${titleId}:${versionId}`;
    const pendingRequest = this.pendingRequests.get(cacheKey);
    if (pendingRequest) {
      return pendingRequest;
    }

    const request = firstValueFrom(
      this.http.get<CardSetDto[]>(
        `/organizations/${organizationId}/titles/${titleId}/versions/${versionId}/card-sets`,
      ),
    )
      .then((cardSets) => {
        this.setCardSets(titleId, versionId, cardSets ?? []);
        return this.getCardSets(titleId, versionId);
      })
      .finally(() => this.pendingRequests.delete(cacheKey));
    this.pendingRequests.set(cacheKey, request);
    return request;
  }

  setCardSets(titleId: string, versionId: string, cardSets: CardSetDto[]): void {
    this.cardSetsByVersion.update((cardSetsByVersion) => ({
      ...cardSetsByVersion,
      [`${titleId}:${versionId}`]: [...cardSets].sort((a, b) => a.name.localeCompare(b.name)),
    }));
  }
}
