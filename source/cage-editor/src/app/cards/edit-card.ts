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
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { firstValueFrom } from 'rxjs';
import { AttributeDto } from '../attributes/attributes-model';
import { AttributesService } from '../attributes/attributes-service';
import { CardSetsService } from '../card-sets/card-sets-service';
import { CardTypeDto, TypedValueDto } from '../card-types/card-types-model';
import {
  formatAttributeValue,
  inheritedCardTypeValues,
  isEditableAttributeType,
  parseAttributeValue,
} from '../card-types/card-type-form-utils';
import { CardTypesService } from '../card-types/card-types-service';
import { EntityValuesEditorComponent } from '../entity-values/entity-values-editor/entity-values-editor.component';
import { TagDto } from '../tags/tags-model';
import { TagsService } from '../tags/tags-service';
import { TitleVersionsService } from '../titles/title-versions-service';
import { CardDto, CardRequestDto } from './cards-model';
import { CardsService } from './cards-service';

interface CardFormModel {
  name: string;
  cardTypeId: string;
  tagIds: string[];
  attributeValues: Record<string, string>;
}

@Component({
  selector: 'edit-card',
  imports: [
    EntityValuesEditorComponent,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './edit-card.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './card-form.scss',
})
export class EditCard implements OnInit {
  private readonly http = inject(HttpClient);
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
  readonly cardId = input.required<string>();
  readonly disabled = input(false);
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
  readonly errorMessage = signal('');
  private readonly preservedAttributes = signal<Record<string, TypedValueDto>>({});
  readonly model = signal<CardFormModel>({
    name: '',
    cardTypeId: '',
    tagIds: [],
    attributeValues: {},
  });
  ngOnInit(): void {
    void this.loadForm();
  }

  setAttributeValues(attributeValues: Record<string, string>): void {
    this.model.update((model) => ({ ...model, attributeValues }));
  }

  setTagIds(tagIds: string[]): void {
    this.model.update((model) => ({ ...model, tagIds }));
  }

  async updateCard(): Promise<void> {
    if (this.disabled() || this.isSubmitting() || this.isReadOnly() || !this.model().cardTypeId) {
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');
    const versionId = this.versionId();
    const cardSetId = this.cardSetId();
    try {
      const attributes: Record<string, TypedValueDto> = { ...this.preservedAttributes() };
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
        name: this.model().name,
        cardTypeId: this.model().cardTypeId,
        attributes,
        tags: this.tags()
          .filter((tag) => this.model().tagIds.includes(tag.id))
          .map((tag) => tag.name),
      };
      await firstValueFrom(
        this.http.put<void>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/card-sets/${cardSetId}/cards/${this.cardId()}`,
          request,
        ),
      );
      const cards = this.cardsService.getCards(this.titleId(), versionId, cardSetId).map((card) =>
        card.id === this.cardId()
          ? {
              ...card,
              ...request,
            }
          : card,
      );
      this.cardsService.setCards(this.titleId(), versionId, cardSetId, cards);
    } catch (error) {
      console.error('Failed to update card.', error);
      this.errorMessage.set(error instanceof Error ? error.message : 'Failed to update card.');
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
      const card = cards.find((item) => item.id === this.cardId());
      if (!version || !cardSet || !card) {
        this.notFound.set(true);
        return;
      }
      this.isReadOnly.set(version.isReadOnly);
      this.attributes.set(attributes);
      this.tags.set(tags);
      this.cardTypes.set(cardTypes);
      const editableAttributeNames = new Set(
        attributes
          .filter((attribute) => isEditableAttributeType(attribute.type))
          .map((attribute) => attribute.name),
      );
      this.preservedAttributes.set(
        Object.fromEntries(
          Object.entries(card.attributes).filter(([name]) => !editableAttributeNames.has(name)),
        ),
      );
      this.model.set(this.toFormModel(card, attributes, tags));
    } catch (error) {
      console.error('Failed to load card form data.', error);
      this.errorMessage.set('Failed to load card form data.');
    } finally {
      this.isLoading.set(false);
    }
  }

  private toFormModel(card: CardDto, attributes: AttributeDto[], tags: TagDto[]): CardFormModel {
    const attributeValues: Record<string, string> = {};
    for (const attribute of attributes.filter((item) => isEditableAttributeType(item.type))) {
      const value = card.attributes[attribute.name];
      if (value) {
        attributeValues[attribute.name] = formatAttributeValue(value);
      }
    }
    return {
      name: card.name,
      cardTypeId: card.cardTypeId,
      tagIds: tags.filter((tag) => card.tags.includes(tag.name)).map((tag) => tag.id),
      attributeValues,
    };
  }
}
