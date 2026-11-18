import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CardSetDto } from '../card-sets/card-sets-model';
import { CardSetsService } from '../card-sets/card-sets-service';
import { CardTypeDto } from '../card-types/card-types-model';
import { CardTypesService } from '../card-types/card-types-service';
import { ConfirmDeleteDialog } from '../confirm-delete-dialog';
import { TitleVersionsService } from '../titles/title-versions-service';
import { CardDto } from './cards-model';
import { CardsService } from './cards-service';

export const maximumCards = 1000;

@Component({
  selector: 'cards',
  imports: [MatProgressSpinnerModule, MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './cards.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './cards.scss',
})
export class Cards implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly cardSetsService = inject(CardSetsService);
  private readonly cardTypesService = inject(CardTypesService);
  private readonly cardsService = inject(CardsService);
  private readonly versionsService = inject(TitleVersionsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly cardSetId = input.required<string>();
  readonly cardSet = signal<CardSetDto | undefined>(undefined);
  readonly cards = signal<CardDto[]>([]);
  readonly cardTypes = signal<CardTypeDto[]>([]);
  readonly searchTerm = signal('');
  readonly filteredCards = computed(() => {
    const searchTerm = this.searchTerm().trim().toLowerCase();
    return this.cards().filter((card) => card.name.toLowerCase().includes(searchTerm));
  });
  readonly isLoading = signal(false);
  readonly isDeleting = signal(false);
  readonly isReadOnly = signal(false);
  readonly notFound = signal(false);
  readonly maximumCards = maximumCards;

  ngOnInit(): void {
    void this.loadCards();
  }

  setSearchTerm(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  displayCardType(card: CardDto): string {
    return this.cardTypes().find((cardType) => cardType.id === card.cardTypeId)?.name ?? '';
  }

  async deleteCard(cardId: string, name: string): Promise<void> {
    if (this.isDeleting() || this.isReadOnly()) {
      return;
    }

    const versionId = this.versionId();
    const cardSetId = this.cardSetId();
    const confirmed = await firstValueFrom(
      this.dialog.open(ConfirmDeleteDialog, { data: { name } }).afterClosed(),
    );
    if (!confirmed) {
      return;
    }

    this.isDeleting.set(true);
    try {
      await firstValueFrom(
        this.http.delete<void>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/card-sets/${cardSetId}/cards/${cardId}`,
        ),
      );
      const cards = this.cards().filter((card) => card.id !== cardId);
      this.cards.set(cards);
      this.cardsService.setCards(this.titleId(), versionId, cardSetId, cards);
      this.cardSetsService.setCardCount(this.titleId(), versionId, cardSetId, cards.length);
    } catch (error) {
      console.error('Failed to delete card.', error);
    } finally {
      this.isDeleting.set(false);
    }
  }

  private async loadCards(): Promise<void> {
    this.isLoading.set(true);
    const versionId = this.versionId();

    try {
      const [versions, cardSets, cardTypes] = await Promise.all([
        this.versionsService.loadVersions(this.organizationId(), this.titleId()),
        this.cardSetsService.loadCardSets(this.organizationId(), this.titleId(), versionId),
        this.cardTypesService.loadCardTypes(this.organizationId(), this.titleId(), versionId),
      ]);
      const version = versions.find((item) => item.id === versionId);
      const cardSet = cardSets.find((item) => item.id === this.cardSetId());
      if (!version || !cardSet) {
        this.notFound.set(true);
        return;
      }

      this.isReadOnly.set(version.isReadOnly);
      this.cardSet.set(cardSet);
      this.cardTypes.set(cardTypes);
      this.cards.set(
        await this.cardsService.loadCards(
          this.organizationId(),
          this.titleId(),
          versionId,
          this.cardSetId(),
        ),
      );
    } catch (error) {
      console.error('Failed to load cards.', error);
      this.cards.set([]);
    } finally {
      this.isLoading.set(false);
    }
  }
}
