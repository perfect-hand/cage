import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AttributeDto, AttributeRequestDto, AttributeType } from './attributes-model';
import { AttributesService, attributeTypes } from './attributes-service';

@Component({
  selector: 'edit-attribute',
  imports: [FormField, MatButtonModule, MatProgressSpinnerModule, RouterLink],
  templateUrl: './edit-attribute.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './edit-attribute.scss',
})
export class EditAttribute implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly attributesService = inject(AttributesService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly attributeId = input.required<string>();
  readonly attributeTypes = attributeTypes;
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly notFound = signal(false);
  readonly model = signal<{ name: string; type: AttributeType }>({ name: '', type: 'Int' });
  readonly attributeForm = form(this.model, (path) => {
    required(path.name, { message: 'Enter an attribute name.' });
    pattern(path.name, /\S/, { message: 'Enter an attribute name.' });
    maxLength(path.name, 50, { message: 'Enter an attribute name of at most 50 characters.' });
  });

  ngOnInit(): void {
    void this.loadAttribute();
  }

  private async loadAttribute(): Promise<void> {
    const versionId = this.versionId();
    let attribute: AttributeDto | undefined;
    try {
      const attributes = await this.attributesService.loadAttributes(
        this.organizationId(),
        this.titleId(),
        versionId,
      );
      attribute = attributes.find((item) => item.id === this.attributeId());
    } catch (error) {
      console.error('Failed to load attributes.', error);
    }

    if (attribute) {
      this.model.set({ name: attribute.name, type: attribute.type });
    } else {
      this.notFound.set(true);
    }
    this.isLoading.set(false);
  }

  async updateAttribute(): Promise<void> {
    if (this.attributeForm().invalid() || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    const versionId = this.versionId();

    try {
      const name = this.model().name.trim();
      const type = this.model().type;
      const attribute: AttributeRequestDto = { name, type };
      await firstValueFrom(
        this.http.put<void>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/attributes/${this.attributeId()}`,
          attribute,
        ),
      );
      const attributes = this.attributesService
        .getAttributes(this.titleId(), versionId)
        .map((attribute) =>
          attribute.id === this.attributeId() ? { ...attribute, name, type } : attribute,
        );
      this.attributesService.setAttributes(this.titleId(), versionId, attributes);
      await this.router.navigate([
        '/organizations',
        this.organizationId(),
        'titles',
        this.titleId(),
        'versions',
        versionId,
        'attributes',
      ]);
    } catch (error) {
      console.error('Failed to update attribute.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
