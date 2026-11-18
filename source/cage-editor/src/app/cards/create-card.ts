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
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AttributeDto } from '../attributes/attributes-model';
import { AttributesService } from '../attributes/attributes-service';
import { CardSetsService } from '../card-sets/card-sets-service';
import { CardTypeDto, TypedValueDto } from '../card-types/card-types-model';
import {
  isEditableAttributeType,
  inheritedCardTypeValues,
  parseAttributeValue,
} from '../card-types/card-type-form-utils';
import { CardTypesService } from '../card-types/card-types-service';
import { EntityValuesEditorComponent } from '../entity-values/entity-values-editor/entity-values-editor.component';
import { TagDto } from '../tags/tags-model';
import { TagsService } from '../tags/tags-service';
import { TitleVersionsService } from '../titles/title-versions-service';
import { CardDto, CardRequestDto } from './cards-model';
import { CardsService } from './cards-service';
import { maximumCards } from './cards';

interface CardFormModel {
  name: string;
  cardTypeId: string;
  tagIds: string[];
  attributeValues: Record<string, string>;
}

@Component({
  selector: 'create-card',
  imports: [
    EntityValuesEditorComponent,
    FormField,
    MatButtonModule,
    MatProgressSpinnerModule,
    RouterLink,
  ],
  templateUrl: './create-card.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './card-form.scss',
})
export class CreateCard implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly attributesService = inject(AttributesService);
  private readonly tagsService = inject(TagsService);
  private readonly cardSetsService = inject(CardSetsService);
  private readonly cardTypesService = inject(CardTypesService);
  private readonly cardsService = inject(CardsService);
  private readonly versionsService = inject(TitleVersionsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly cardSetId = input.required<string>();
  readonly attributes = signal<AttributeDto[]>([]);
  readonly tags = signal<TagDto[]>([]);
  readonly cardTypes = signal<CardTypeDto[]>([]);
  readonly inheritedValues = computed(() =>
    inheritedCardTypeValues(this.model().cardTypeId, this.cardTypes()),
  );
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly isReadOnly = signal(false);
  readonly notFound = signal(false);
  readonly atLimit = signal(false);
  readonly errorMessage = signal('');
  readonly maximumCards = maximumCards;
  readonly model = signal<CardFormModel>({
    name: '',
    cardTypeId: '',
    tagIds: [],
    attributeValues: {},
  });
  readonly cardForm = form(this.model, (path) => {
    required(path.name, { message: 'Enter a card name.' });
    pattern(path.name, /\S/, { message: 'Enter a card name.' });
    maxLength(path.name, 50, { message: 'Enter a card name of at most 50 characters.' });
  });

  ngOnInit(): void {
    void this.loadForm();
  }

  setCardTypeId(event: Event): void {
    this.model.update((model) => ({
      ...model,
      cardTypeId: (event.target as HTMLSelectElement).value,
    }));
  }

  setAttributeValues(attributeValues: Record<string, string>): void {
    this.model.update((model) => ({ ...model, attributeValues }));
  }

  setTagIds(tagIds: string[]): void {
    this.model.update((model) => ({ ...model, tagIds }));
  }

  cardsPath(): string[] {
    return [
      '/organizations',
      this.organizationId(),
      'titles',
      this.titleId(),
      'versions',
      this.versionId(),
      'card-sets',
      this.cardSetId(),
      'cards',
    ];
  }

  async createCard(): Promise<void> {
    if (
      this.cardForm().invalid() ||
      this.isSubmitting() ||
      this.atLimit() ||
      this.isReadOnly() ||
      !this.model().cardTypeId
    ) {
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');
    const versionId = this.versionId();
    const cardSetId = this.cardSetId();
    try {
      const cards = await this.cardsService.loadCards(
        this.organizationId(),
        this.titleId(),
        versionId,
        cardSetId,
      );
      if (cards.length >= maximumCards) {
        this.atLimit.set(true);
        return;
      }

      const attributes: Record<string, TypedValueDto> = {};
      for (const definition of this.attributes()) {
        if (!isEditableAttributeType(definition.type)) {
          continue;
        }

        const value = this.model().attributeValues[definition.name];
        if (value !== undefined) {
          attributes[definition.name] = parseAttributeValue(definition.type, value);
        }
      }
      const request: CardRequestDto = {
        name: this.model().name.trim(),
        cardTypeId: this.model().cardTypeId,
        attributes,
        tags: this.tags()
          .filter((tag) => this.model().tagIds.includes(tag.id))
          .map((tag) => tag.name),
      };
      const card = await firstValueFrom(
        this.http.post<CardDto>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/card-sets/${cardSetId}/cards`,
          request,
        ),
      );
      this.cardsService.setCards(this.titleId(), versionId, cardSetId, [...cards, card]);
      this.cardSetsService.setCardCount(this.titleId(), versionId, cardSetId, cards.length + 1);
      await this.router.navigate(this.cardsPath());
    } catch (error) {
      console.error('Failed to create card.', error);
      this.errorMessage.set(error instanceof Error ? error.message : 'Failed to create card.');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private async loadForm(): Promise<void> {
    const versionId = this.versionId();
    const cardSetId = this.cardSetId();
    try {
      const [versions, cardSets, attributes, tags, cardTypes, cards] = await Promise.all([
        this.versionsService.loadVersions(this.organizationId(), this.titleId()),
        this.cardSetsService.loadCardSets(this.organizationId(), this.titleId(), versionId),
        this.attributesService.loadAttributes(this.organizationId(), this.titleId(), versionId),
        this.tagsService.loadTags(this.organizationId(), this.titleId(), versionId),
        this.cardTypesService.loadCardTypes(this.organizationId(), this.titleId(), versionId),
        this.cardsService.loadCards(this.organizationId(), this.titleId(), versionId, cardSetId),
      ]);
      const version = versions.find((item) => item.id === versionId);
      const cardSet = cardSets.find((item) => item.id === cardSetId);
      if (!version || !cardSet) {
        this.notFound.set(true);
        return;
      }
      this.isReadOnly.set(version.isReadOnly);
      this.atLimit.set(cards.length >= maximumCards);
      this.attributes.set(attributes);
      this.tags.set(tags);
      this.cardTypes.set(cardTypes);
    } catch (error) {
      console.error('Failed to load card form data.', error);
      this.errorMessage.set('Failed to load card form data.');
    } finally {
      this.isLoading.set(false);
    }
  }
}
