import { ChangeDetectionStrategy, Component, computed, input, model, signal } from '@angular/core';
import {
  MatAutocompleteModule,
  MatAutocompleteSelectedEvent,
} from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AttributeDto } from '../../attributes/attributes-model';
import {
  defaultAttributeValue,
  formatAttributeValue,
  isEditableAttributeType,
} from '../../card-types/card-type-form-utils';
import { TypedValueDto } from '../../card-types/card-types-model';
import { TagDto } from '../../tags/tags-model';

@Component({
  selector: 'app-entity-values-editor',
  imports: [MatAutocompleteModule, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './entity-values-editor.component.html',
  styleUrl: './entity-values-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class EntityValuesEditorComponent {
  readonly attributes = input.required<AttributeDto[]>();
  readonly tags = input.required<TagDto[]>();
  readonly inheritedAttributes = input.required<ReadonlyMap<string, TypedValueDto>>();
  readonly inheritedTags = input.required<ReadonlySet<string>>();
  readonly attributeValues = model.required<Record<string, string>>();
  readonly tagIds = model.required<string[]>();
  readonly attributeQuery = signal('');
  readonly tagQuery = signal('');

  readonly editableAttributes = computed(() =>
    this.attributes().filter((attribute) => isEditableAttributeType(attribute.type)),
  );
  readonly selectedAttributes = computed(() =>
    this.editableAttributes().filter(
      (attribute) =>
        Object.hasOwn(this.attributeValues(), attribute.name) ||
        this.inheritedAttributes().has(attribute.name),
    ),
  );
  readonly availableAttributes = computed(() =>
    this.editableAttributes().filter(
      (attribute) =>
        !Object.hasOwn(this.attributeValues(), attribute.name) &&
        !this.inheritedAttributes().has(attribute.name) &&
        attribute.name.toLowerCase().includes(this.attributeQuery().trim().toLowerCase()),
    ),
  );
  readonly selectedTags = computed(() =>
    this.tags().filter(
      (tag) => this.tagIds().includes(tag.id) || this.inheritedTags().has(tag.name),
    ),
  );
  readonly availableTags = computed(() =>
    this.tags().filter(
      (tag) =>
        !this.tagIds().includes(tag.id) &&
        !this.inheritedTags().has(tag.name) &&
        tag.name.toLowerCase().includes(this.tagQuery().trim().toLowerCase()),
    ),
  );

  attributeValue(name: string): string {
    const value = this.attributeValues()[name];
    if (value !== undefined) {
      return value;
    }
    const inherited = this.inheritedAttributes().get(name);
    return inherited ? formatAttributeValue(inherited) : '';
  }

  addAttribute(event: MatAutocompleteSelectedEvent, field: HTMLInputElement): void {
    const attribute = this.editableAttributes().find((item) => item.id === event.option.value);
    if (
      attribute &&
      isEditableAttributeType(attribute.type) &&
      !Object.hasOwn(this.attributeValues(), attribute.name) &&
      !this.inheritedAttributes().has(attribute.name)
    ) {
      const value = defaultAttributeValue(attribute.type);
      this.attributeValues.update((values) => ({
        ...values,
        [attribute.name]: value,
      }));
    }
    field.value = '';
    this.attributeQuery.set('');
  }

  removeAttribute(name: string): void {
    if (this.inheritedAttributes().has(name)) return;
    this.attributeValues.update((values) => {
      const updated = { ...values };
      delete updated[name];
      return updated;
    });
  }

  setAttributeValue(name: string, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.attributeValues.update((values) => ({ ...values, [name]: value }));
  }

  addTag(event: MatAutocompleteSelectedEvent, field: HTMLInputElement): void {
    const tag = this.tags().find((item) => item.id === event.option.value);
    if (tag && !this.tagIds().includes(tag.id) && !this.inheritedTags().has(tag.name)) {
      this.tagIds.update((ids) => [...ids, tag.id]);
    }
    field.value = '';
    this.tagQuery.set('');
  }

  removeTag(tag: TagDto): void {
    if (this.inheritedTags().has(tag.name)) return;
    this.tagIds.update((ids) => ids.filter((id) => id !== tag.id));
  }
}
