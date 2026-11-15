import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CardSetDto, CardSetRequestDto } from './card-sets-model';
import { CardSetsService } from './card-sets-service';

@Component({
  selector: 'edit-card-set',
  imports: [FormField, MatButtonModule, MatProgressSpinnerModule, RouterLink],
  templateUrl: './edit-card-set.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './card-set-form.scss',
})
export class EditCardSet implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly cardSetsService = inject(CardSetsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly cardSetId = input.required<string>();
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly notFound = signal(false);
  readonly model = signal({ name: '' });
  readonly cardSetForm = form(this.model, (path) => {
    required(path.name, { message: 'Enter a card set name.' });
    pattern(path.name, /\S/, { message: 'Enter a card set name.' });
    maxLength(path.name, 50, { message: 'Enter a card set name of at most 50 characters.' });
  });

  ngOnInit(): void {
    void this.loadCardSet();
  }

  private async loadCardSet(): Promise<void> {
    const versionId = this.versionId();
    let cardSet: CardSetDto | undefined;
    try {
      const cardSets = await this.cardSetsService.loadCardSets(
        this.organizationId(),
        this.titleId(),
        versionId,
      );
      cardSet = cardSets.find((item) => item.id === this.cardSetId());
    } catch (error) {
      console.error('Failed to load card sets.', error);
    }

    if (cardSet) {
      this.model.set({ name: cardSet.name });
    } else {
      this.notFound.set(true);
    }
    this.isLoading.set(false);
  }

  async updateCardSet(): Promise<void> {
    if (this.cardSetForm().invalid() || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    const versionId = this.versionId();
    try {
      const name = this.model().name.trim();
      const cardSet: CardSetRequestDto = { name };
      await firstValueFrom(
        this.http.put<void>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/card-sets/${this.cardSetId()}`,
          cardSet,
        ),
      );
      const cardSets = this.cardSetsService
        .getCardSets(this.titleId(), versionId)
        .map((item) => (item.id === this.cardSetId() ? { ...item, name } : item));
      this.cardSetsService.setCardSets(this.titleId(), versionId, cardSets);
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
      console.error('Failed to update card set.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
