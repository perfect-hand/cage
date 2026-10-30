import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { maximumAttributes } from './attributes';
import { AttributesService, AttributeType, attributeTypes } from './attributes-service';

@Component({
  selector: 'create-attribute',
  imports: [FormField, MatButtonModule, RouterLink],
  templateUrl: './create-attribute.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './create-attribute.scss',
})
export class CreateAttribute {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly attributesService = inject(AttributesService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly attributeTypes = attributeTypes;
  readonly maximumAttributes = maximumAttributes;
  readonly isSubmitting = signal(false);
  readonly atLimit = signal(false);
  readonly model = signal<{ name: string; type: AttributeType }>({ name: '', type: 'Int' });
  readonly attributeForm = form(this.model, (path) => {
    required(path.name, { message: 'Enter an attribute name.' });
    pattern(path.name, /\S/, { message: 'Enter an attribute name.' });
    maxLength(path.name, 50, { message: 'Enter an attribute name of at most 50 characters.' });
  });

  async createAttribute(): Promise<void> {
    if (this.attributeForm().invalid() || this.isSubmitting() || this.atLimit()) {
      return;
    }

    this.isSubmitting.set(true);

    try {
      if (this.attributesService.getAttributes(this.titleId()).length >= maximumAttributes) {
        this.atLimit.set(true);
        return;
      }

      await firstValueFrom(
        this.http.post(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/attributes`,
          { name: this.model().name.trim(), type: this.model().type },
        ),
      );
      await this.router.navigate([
        '/organizations',
        this.organizationId(),
        'titles',
        this.titleId(),
        'attributes',
      ]);
    } catch (error) {
      console.error('Failed to create attribute.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
