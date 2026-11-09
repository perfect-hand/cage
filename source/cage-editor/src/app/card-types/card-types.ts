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
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CardTypeDto } from './card-types-model';
import { CardTypesService } from './card-types-service';
import { ConfirmDeleteDialog } from '../confirm-delete-dialog';
import { TitleVersionsService } from '../titles/title-versions-service';

export const maximumCardTypes = 100;

@Component({
  selector: 'card-types',
  imports: [MatProgressSpinnerModule, MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './card-types.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './card-types.scss',
})
export class CardTypes implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly cardTypesService = inject(CardTypesService);
  private readonly versionsService = inject(TitleVersionsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly cardTypes = signal<CardTypeDto[]>([]);
  readonly searchTerm = signal('');
  readonly filteredCardTypes = computed(() => {
    const searchTerm = this.searchTerm().trim().toLowerCase();
    return this.cardTypes().filter((cardType) => cardType.name.toLowerCase().includes(searchTerm));
  });
  readonly isLoading = signal(false);
  readonly isDeleting = signal(false);
  readonly isReadOnly = signal(false);
  readonly notFound = signal(false);
  readonly maximumCardTypes = maximumCardTypes;

  ngOnInit(): void {
    void this.loadCardTypes();
  }

  setSearchTerm(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  displayAttributes(cardType: CardTypeDto): string {
    return Object.keys(cardType.attributes).join(', ');
  }

  displayParent(cardType: CardTypeDto): string {
    return this.cardTypes().find((item) => item.id === cardType.parentId)?.name ?? '';
  }

  async deleteCardType(cardTypeId: string, name: string): Promise<void> {
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
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/card-types/${cardTypeId}`,
        ),
      );
      const cardTypes = this.cardTypes().filter((cardType) => cardType.id !== cardTypeId);
      this.cardTypes.set(cardTypes);
      this.cardTypesService.setCardTypes(this.titleId(), versionId, cardTypes);
    } catch (error) {
      console.error('Failed to delete card type.', error);
    } finally {
      this.isDeleting.set(false);
    }
  }

  private async loadCardTypes(): Promise<void> {
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
      this.cardTypes.set(
        await this.cardTypesService.loadCardTypes(this.organizationId(), this.titleId(), versionId),
      );
    } catch (error) {
      console.error('Failed to load card types.', error);
      this.cardTypes.set([]);
    } finally {
      this.isLoading.set(false);
    }
  }
}
