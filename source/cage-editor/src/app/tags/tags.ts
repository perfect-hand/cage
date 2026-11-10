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
import { ConfirmDeleteDialog } from '../confirm-delete-dialog';
import { TitleVersionsService } from '../titles/title-versions-service';
import { TagDto } from './tags-model';
import { TagsService } from './tags-service';

export const maximumTags = 100;

@Component({
  selector: 'tags',
  imports: [MatProgressSpinnerModule, MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './tags.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './tags.scss',
})
export class Tags implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly tagsService = inject(TagsService);
  private readonly versionsService = inject(TitleVersionsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly tags = signal<TagDto[]>([]);
  readonly searchTerm = signal('');
  readonly filteredTags = computed(() => {
    const searchTerm = this.searchTerm().trim().toLowerCase();
    return this.tags().filter((tag) => tag.name.toLowerCase().includes(searchTerm));
  });
  readonly isLoading = signal(false);
  readonly isDeleting = signal(false);
  readonly isReadOnly = signal(false);
  readonly notFound = signal(false);
  readonly maximumTags = maximumTags;

  ngOnInit(): void {
    void this.loadTags();
  }

  setSearchTerm(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  private async loadTags(): Promise<void> {
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

      const tags = await this.tagsService.loadTags(
        this.organizationId(),
        this.titleId(),
        versionId,
      );
      this.tags.set(tags);
    } catch (error) {
      console.error('Failed to load tags.', error);
      this.tags.set([]);
    } finally {
      this.isLoading.set(false);
    }
  }

  async deleteTag(tagId: string, name: string, referenceCount: number): Promise<void> {
    if (this.isDeleting() || this.isReadOnly()) {
      return;
    }

    const versionId = this.versionId();
    const confirmed = await firstValueFrom(
      this.dialog
        .open(ConfirmDeleteDialog, {
          data: {
            name,
            consequence: referenceCount > 0
              ? `This tag is referenced by ${referenceCount} card type${referenceCount === 1 ? '' : 's'}. Deleting it will remove the tag from those card types.`
              : undefined,
          },
        })
        .afterClosed(),
    );
    if (!confirmed) {
      return;
    }

    this.isDeleting.set(true);
    try {
      await firstValueFrom(
        this.http.delete<void>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/tags/${tagId}`,
        ),
      );
      const tags = this.tags().filter((tag) => tag.id !== tagId);
      this.tags.set(tags);
      this.tagsService.setTags(this.titleId(), versionId, tags);
    } catch (error) {
      console.error('Failed to delete tag.', error);
    } finally {
      this.isDeleting.set(false);
    }
  }
}
