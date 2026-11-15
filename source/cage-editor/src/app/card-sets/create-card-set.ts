import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { maximumCardSets } from './card-sets';
import { CardSetDto, CardSetRequestDto } from './card-sets-model';
import { CardSetsService } from './card-sets-service';

@Component({
  selector: 'create-card-set',
  imports: [FormField, MatButtonModule, RouterLink],
  templateUrl: './create-card-set.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './card-set-form.scss',
})
export class CreateCardSet {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly cardSetsService = inject(CardSetsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly maximumCardSets = maximumCardSets;
  readonly isSubmitting = signal(false);
  readonly atLimit = signal(false);
  readonly model = signal({ name: '' });
  readonly cardSetForm = form(this.model, (path) => {
    required(path.name, { message: 'Enter a card set name.' });
    pattern(path.name, /\S/, { message: 'Enter a card set name.' });
    maxLength(path.name, 50, { message: 'Enter a card set name of at most 50 characters.' });
  });

  async createCardSet(): Promise<void> {
    if (this.cardSetForm().invalid() || this.isSubmitting() || this.atLimit()) {
      return;
    }

    this.isSubmitting.set(true);
    const versionId = this.versionId();
    try {
      const cardSets = await this.cardSetsService.loadCardSets(
        this.organizationId(),
        this.titleId(),
        versionId,
      );
      if (cardSets.length >= maximumCardSets) {
        this.atLimit.set(true);
        return;
      }

      const cardSet: CardSetRequestDto = { name: this.model().name.trim() };
      const createdCardSet = await firstValueFrom(
        this.http.post<CardSetDto>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/card-sets`,
          cardSet,
        ),
      );
      this.cardSetsService.setCardSets(this.titleId(), versionId, [...cardSets, createdCardSet]);
      await this.router.navigate([
        '/organizations',
        this.organizationId(),
        'titles',
        this.titleId(),
        'versions',
        versionId,
        'card-sets',
      ]);
    } catch (error) {
      console.error('Failed to create card set.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
