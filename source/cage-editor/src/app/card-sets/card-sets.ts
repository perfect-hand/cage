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
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ConfirmDeleteDialog } from '../confirm-delete-dialog';
import { TitleVersionsService } from '../titles/title-versions-service';
import { CardSetDto } from './card-sets-model';
import { CardSetsService } from './card-sets-service';

export const maximumCardSets = 100;

@Component({
  selector: 'card-sets',
  imports: [MatProgressSpinnerModule, MatButtonModule, MatIconModule, MatTooltipModule, RouterLink],
  templateUrl: './card-sets.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './card-sets.scss',
})
export class CardSets implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly cardSetsService = inject(CardSetsService);
  private readonly versionsService = inject(TitleVersionsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly cardSets = signal<CardSetDto[]>([]);
  readonly searchTerm = signal('');
  readonly filteredCardSets = computed(() => {
    const searchTerm = this.searchTerm().trim().toLowerCase();
    return this.cardSets().filter((cardSet) => cardSet.name.toLowerCase().includes(searchTerm));
  });
  readonly isLoading = signal(false);
  readonly isDeleting = signal(false);
  readonly isReadOnly = signal(false);
  readonly notFound = signal(false);
  readonly maximumCardSets = maximumCardSets;

  ngOnInit(): void {
    void this.loadCardSets();
  }

  setSearchTerm(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  private async loadCardSets(): Promise<void> {
    this.isLoading.set(true);
    const versionId = this.versionId();

    try {
      const versions = await this.versionsService.loadVersions(
        this.organizationId(),
        this.titleId(),
      );
      const version = versions.find((item) => item.id === versionId);
      if (!version) {
        this.notFound.set(true);
        return;
      }
      this.isReadOnly.set(version.isReadOnly);

      const cardSets = await this.cardSetsService.loadCardSets(
        this.organizationId(),
        this.titleId(),
        versionId,
      );
      this.cardSets.set(cardSets);
    } catch (error) {
      console.error('Failed to load card sets.', error);
      this.cardSets.set([]);
    } finally {
      this.isLoading.set(false);
    }
  }

  async deleteCardSet(cardSetId: string, name: string): Promise<void> {
    if (this.isDeleting() || this.isReadOnly()) {
      return;
    }

    const versionId = this.versionId();
    const confirmed = await firstValueFrom(
      this.dialog.open(ConfirmDeleteDialog, { data: { name } }).afterClosed(),
    );
    if (!confirmed) {
      return;
    }

    this.isDeleting.set(true);
    try {
      await firstValueFrom(
        this.http.delete<void>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/card-sets/${cardSetId}`,
        ),
      );
      const cardSets = this.cardSets().filter((cardSet) => cardSet.id !== cardSetId);
      this.cardSets.set(cardSets);
      this.cardSetsService.setCardSets(this.titleId(), versionId, cardSets);
    } catch (error) {
      console.error('Failed to delete card set.', error);
    } finally {
      this.isDeleting.set(false);
    }
  }
}
