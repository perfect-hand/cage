import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TagDto, TagRequestDto } from './tags-model';
import { TagsService } from './tags-service';

@Component({
  selector: 'edit-tag',
  imports: [FormField, MatButtonModule, MatProgressSpinnerModule, RouterLink],
  templateUrl: './edit-tag.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './tag-form.scss',
})
export class EditTag implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tagsService = inject(TagsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly tagId = input.required<string>();
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly notFound = signal(false);
  readonly model = signal({ name: '' });
  readonly tagForm = form(this.model, (path) => {
    required(path.name, { message: 'Enter a tag name.' });
    pattern(path.name, /\S/, { message: 'Enter a tag name.' });
    maxLength(path.name, 50, { message: 'Enter a tag name of at most 50 characters.' });
  });

  ngOnInit(): void {
    void this.loadTag();
  }

  private async loadTag(): Promise<void> {
    const versionId = this.versionId();
    let tags = this.tagsService.getTags(this.titleId(), versionId);
    let tag = tags.find((item) => item.id === this.tagId());
    if (!tag) {
      try {
        tags =
          (await firstValueFrom(
            this.http.get<TagDto[]>(
              `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/tags`,
            ),
          )) ?? [];
        this.tagsService.setTags(this.titleId(), versionId, tags);
        tag = tags.find((item) => item.id === this.tagId());
      } catch (error) {
        console.error('Failed to load tags.', error);
      }
    }

    if (tag) {
      this.model.set({ name: tag.name });
    } else {
      this.notFound.set(true);
    }
    this.isLoading.set(false);
  }

  async updateTag(): Promise<void> {
    if (this.tagForm().invalid() || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    const versionId = this.versionId();
    try {
      const name = this.model().name.trim();
      const tag: TagRequestDto = { name };
      await firstValueFrom(
        this.http.put<void>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/tags/${this.tagId()}`,
          tag,
        ),
      );
      const tags = this.tagsService
        .getTags(this.titleId(), versionId)
        .map((item) => (item.id === this.tagId() ? { ...item, name } : item));
      this.tagsService.setTags(this.titleId(), versionId, tags);
      await this.router.navigate([
        '/organizations',
        this.organizationId(),
        'titles',
        this.titleId(),
        'versions',
        versionId,
        'tags',
      ]);
    } catch (error) {
      console.error('Failed to update tag.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
