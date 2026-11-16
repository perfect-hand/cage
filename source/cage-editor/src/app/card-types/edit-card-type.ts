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
import { EntityValuesEditorComponent } from '../entity-values/entity-values-editor/entity-values-editor.component';
import { TagDto } from '../tags/tags-model';
import { TagsService } from '../tags/tags-service';
import { TitleVersionsService } from '../titles/title-versions-service';
import { CardTypeDto, CardTypeRequestDto, TypedValueDto } from './card-types-model';
import { CardTypesService } from './card-types-service';
import {
  formatAttributeValue,
  inheritedCardTypeValues,
  isEditableAttributeType,
  parseAttributeValue,
} from './card-type-form-utils';

interface CardTypeFormModel {
  name: string;
  parentId: string;
  tagIds: string[];
  attributeValues: Record<string, string>;
}

@Component({
  selector: 'edit-card-type',
  imports: [
    FormField,
    MatButtonModule,
    EntityValuesEditorComponent,
    MatProgressSpinnerModule,
    RouterLink,
  ],
  templateUrl: './edit-card-type.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './card-type-form.scss',
})
export class EditCardType implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly attributesService = inject(AttributesService);
  private readonly tagsService = inject(TagsService);
  private readonly cardTypesService = inject(CardTypesService);
  private readonly versionsService = inject(TitleVersionsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly cardTypeId = input.required<string>();
  readonly attributes = signal<AttributeDto[]>([]);
  readonly editableAttributes = computed(() =>
    this.attributes().filter((attribute) => isEditableAttributeType(attribute.type)),
  );
  readonly tags = signal<TagDto[]>([]);
  readonly parentCardTypes = signal<CardTypeDto[]>([]);
  readonly inheritedValues = computed(() =>
    inheritedCardTypeValues(this.model().parentId, this.parentCardTypes()),
  );
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly isReadOnly = signal(false);
  readonly notFound = signal(false);
  readonly errorMessage = signal('');
  private readonly preservedAttributes = signal<Record<string, TypedValueDto>>({});
  readonly model = signal<CardTypeFormModel>({
    name: '',
    parentId: '',
    tagIds: [],
    attributeValues: {},
  });
  readonly cardTypeForm = form(this.model, (path) => {
    required(path.name, { message: 'Enter a card type name.' });
    pattern(path.name, /\S/, { message: 'Enter a card type name.' });
    maxLength(path.name, 50, { message: 'Enter a card type name of at most 50 characters.' });
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

  setParentId(event: Event): void {
    this.model.update((model) => ({
      ...model,
      parentId: (event.target as HTMLSelectElement).value,
    }));
  }

  async updateCardType(): Promise<void> {
    if (this.cardTypeForm().invalid() || this.isSubmitting() || this.isReadOnly()) {
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');
    const versionId = this.versionId();
    try {
      const attributes: Record<string, TypedValueDto> = { ...this.preservedAttributes() };
      for (const definition of this.editableAttributes()) {
        if (!isEditableAttributeType(definition.type)) {
          continue;
        }

        const value = this.model().attributeValues[definition.name];
        if (value !== undefined) {
          attributes[definition.name] = parseAttributeValue(definition.type, value);
        }
      }
      const request: CardTypeRequestDto = {
        name: this.model().name.trim(),
        parentId: this.model().parentId || null,
        attributes,
        tags: this.tags()
          .filter((tag) => this.model().tagIds.includes(tag.id))
          .map((tag) => tag.name),
      };
      await firstValueFrom(
        this.http.put<void>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/card-types/${this.cardTypeId()}`,
          request,
        ),
      );
      const cardTypes = this.cardTypesService
        .getCardTypes(this.titleId(), versionId)
        .map((cardType) =>
          cardType.id === this.cardTypeId()
            ? {
                ...cardType,
                ...request,
              }
            : cardType,
        );
      this.cardTypesService.setCardTypes(this.titleId(), versionId, cardTypes);
      await this.router.navigate([
        '/organizations',
        this.organizationId(),
        'titles',
        this.titleId(),
        'versions',
        versionId,
        'card-types',
      ]);
    } catch (error) {
      console.error('Failed to update card type.', error);
      this.errorMessage.set(error instanceof Error ? error.message : 'Failed to update card type.');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private async loadForm(): Promise<void> {
    const versionId = this.versionId();
    try {
      const [versions, attributes, tags, cardTypes] = await Promise.all([
        this.versionsService.loadVersions(this.organizationId(), this.titleId()),
        this.attributesService.loadAttributes(this.organizationId(), this.titleId(), versionId),
        this.tagsService.loadTags(this.organizationId(), this.titleId(), versionId),
        this.cardTypesService.loadCardTypes(this.organizationId(), this.titleId(), versionId),
      ]);
      const version = versions.find((item) => item.id === versionId);
      const cardType = cardTypes.find((item) => item.id === this.cardTypeId());
      if (!version || !cardType) {
        this.notFound.set(true);
        return;
      }
      this.isReadOnly.set(version.isReadOnly);
      this.attributes.set(attributes);
      this.tags.set(tags);
      this.parentCardTypes.set(cardTypes.filter((item) => item.id !== cardType.id));
      const editableAttributeNames = new Set(
        attributes
          .filter((attribute) => isEditableAttributeType(attribute.type))
          .map((attribute) => attribute.name),
      );
      this.preservedAttributes.set(
        Object.fromEntries(
          Object.entries(cardType.attributes).filter(([name]) => !editableAttributeNames.has(name)),
        ),
      );
      this.model.set(this.toFormModel(cardType, attributes, tags));
    } catch (error) {
      console.error('Failed to load card type form data.', error);
      this.errorMessage.set('Failed to load card type form data.');
    } finally {
      this.isLoading.set(false);
    }
  }

  private toFormModel(
    cardType: CardTypeDto,
    attributes: AttributeDto[],
    tags: TagDto[],
  ): CardTypeFormModel {
    const attributeValues: Record<string, string> = {};
    for (const attribute of attributes.filter((item) => isEditableAttributeType(item.type))) {
      const value = cardType.attributes[attribute.name];
      if (value) {
        attributeValues[attribute.name] = formatAttributeValue(value);
      }
    }
    return {
      name: cardType.name,
      parentId: cardType.parentId ?? '',
      tagIds: tags.filter((tag) => cardType.tags.includes(tag.name)).map((tag) => tag.id),
      attributeValues,
    };
  }
}
