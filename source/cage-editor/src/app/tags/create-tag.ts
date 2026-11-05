import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { maximumTags } from './tags';
import { TagDto, TagRequestDto } from './tags-model';
import { TagsService } from './tags-service';

@Component({
  selector: 'create-tag',
  imports: [FormField, MatButtonModule, RouterLink],
  templateUrl: './create-tag.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './tag-form.scss',
})
export class CreateTag {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tagsService = inject(TagsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly maximumTags = maximumTags;
  readonly isSubmitting = signal(false);
  readonly atLimit = signal(false);
  readonly model = signal({ name: '' });
  readonly tagForm = form(this.model, (path) => {
    required(path.name, { message: 'Enter a tag name.' });
    pattern(path.name, /\S/, { message: 'Enter a tag name.' });
    maxLength(path.name, 50, { message: 'Enter a tag name of at most 50 characters.' });
  });

  async createTag(): Promise<void> {
    if (this.tagForm().invalid() || this.isSubmitting() || this.atLimit()) {
      return;
    }

    this.isSubmitting.set(true);
    const versionId = this.versionId();
    try {
      if (this.tagsService.getTags(this.titleId(), versionId).length >= maximumTags) {
        this.atLimit.set(true);
        return;
      }

      const tag: TagRequestDto = { name: this.model().name.trim() };
      await firstValueFrom(
        this.http.post<TagDto>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/tags`,
          tag,
        ),
      );
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
      console.error('Failed to create tag.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
