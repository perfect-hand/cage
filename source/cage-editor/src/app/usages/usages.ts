import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { AttributesService } from '../attributes/attributes-service';
import { CardSetsService } from '../card-sets/card-sets-service';
import { CardTypeDto } from '../card-types/card-types-model';
import { CardTypesService } from '../card-types/card-types-service';
import { CardDto } from '../cards/cards-model';
import { CardsService } from '../cards/cards-service';
import { TagsService } from '../tags/tags-service';

@Component({
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [MatProgressSpinnerModule, RouterLink],
  templateUrl: './usages.html',
})
export class Usages implements OnInit {
  private readonly attributesService = inject(AttributesService);
  private readonly tagsService = inject(TagsService);
  private readonly cardTypesService = inject(CardTypesService);
  private readonly cardSetsService = inject(CardSetsService);
  private readonly cardsService = inject(CardsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly attributeId = input<string>();
  readonly tagId = input<string>();
  readonly name = signal('');
  readonly cardTypes = signal<CardTypeDto[]>([]);
  readonly cards = signal<{ card: CardDto; cardSetId: string }[]>([]);
  readonly isLoading = signal(true);
  readonly notFound = signal(false);
  readonly error = signal(false);

  ngOnInit(): void {
    void this.loadUsages();
  }

  private async loadUsages(): Promise<void> {
    const organizationId = this.organizationId();
    const titleId = this.titleId();
    const versionId = this.versionId();
    const attributeId = this.attributeId();

    try {
      const entities = attributeId
        ? await this.attributesService.loadAttributes(organizationId, titleId, versionId)
        : await this.tagsService.loadTags(organizationId, titleId, versionId);
      const entity = entities.find((item) => item.id === (attributeId ?? this.tagId()));
      if (!entity) {
        this.notFound.set(true);
        return;
      }

      this.name.set(entity.name);
      const [cardTypes, cardSets] = await Promise.all([
        this.cardTypesService.loadCardTypes(organizationId, titleId, versionId),
        this.cardSetsService.loadCardSets(organizationId, titleId, versionId),
      ]);
      const referencesEntity = (item: Pick<CardDto, 'attributes' | 'tags'>) =>
        attributeId
          ? Object.prototype.hasOwnProperty.call(item.attributes, entity.name)
          : item.tags.includes(entity.name);
      this.cardTypes.set(cardTypes.filter(referencesEntity));
      const cardsBySet = await Promise.all(
        cardSets.map(async (cardSet) => ({
          cardSetId: cardSet.id,
          cards: await this.cardsService.loadCards(organizationId, titleId, versionId, cardSet.id),
        })),
      );
      this.cards.set(cardsBySet.flatMap(({ cardSetId, cards }) =>
        cards.filter(referencesEntity).map((card) => ({ card, cardSetId })),
      ));
    } catch (error) {
      console.error('Failed to load usages.', error);
      this.error.set(true);
    } finally {
      this.isLoading.set(false);
    }
  }
}
