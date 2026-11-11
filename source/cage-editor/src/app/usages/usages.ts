import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { AttributesService } from '../attributes/attributes-service';
import { CardTypeDto } from '../card-types/card-types-model';
import { CardTypesService } from '../card-types/card-types-service';
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

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly attributeId = input<string>();
  readonly tagId = input<string>();
  readonly name = signal('');
  readonly cardTypes = signal<CardTypeDto[]>([]);
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
      const cardTypes = await this.cardTypesService.loadCardTypes(organizationId, titleId, versionId);
      this.cardTypes.set(cardTypes.filter((cardType) =>
        attributeId
          ? Object.prototype.hasOwnProperty.call(cardType.attributes, entity.name)
          : cardType.tags.includes(entity.name),
      ));
    } catch (error) {
      console.error('Failed to load usages.', error);
      this.error.set(true);
    } finally {
      this.isLoading.set(false);
    }
  }
}
