import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { AttributesService } from '../attributes/attributes-service';
import { TagsService } from '../tags/tags-service';
import { CardTypesService } from '../card-types/card-types-service';
import { TitleVersionDto } from './title-versions-model';
import { TitleVersionsService } from './title-versions-service';

@Component({
  selector: 'title-version-overview',
  imports: [DatePipe, MatProgressSpinnerModule, RouterLink],
  templateUrl: './title-version-overview.html',
  styleUrl: './title-version-overview.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class TitleVersionOverview implements OnInit {
  private readonly versionsService = inject(TitleVersionsService);
  private readonly attributesService = inject(AttributesService);
  private readonly tagsService = inject(TagsService);
  private readonly cardTypesService = inject(CardTypesService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  private readonly versions = signal<TitleVersionDto[]>([]);

  readonly version = signal<TitleVersionDto | undefined>(undefined);
  readonly parentVersionName = computed(() => {
    const parentVersionId = this.version()?.parentVersionId;
    return parentVersionId
      ? this.versions().find((version) => version.id === parentVersionId)?.name
      : undefined;
  });
  readonly attributeCount = signal(0);
  readonly tagCount = signal(0);
  readonly cardTypeCount = signal(0);
  readonly isLoading = signal(true);
  readonly notFound = signal(false);

  ngOnInit(): void {
    void this.loadOverview();
  }

  private async loadOverview(): Promise<void> {
    try {
      const versions = await this.versionsService.loadVersions(
        this.organizationId(),
        this.titleId(),
      );
      this.versions.set(versions);

      const version = versions.find((item) => item.id === this.versionId());
      if (!version) {
        this.notFound.set(true);
        return;
      }
      this.version.set(version);

      const [attributes, tags, cardTypes] = await Promise.all([
        this.attributesService.loadAttributes(this.organizationId(), this.titleId(), version.id),
        this.tagsService.loadTags(this.organizationId(), this.titleId(), version.id),
        this.cardTypesService.loadCardTypes(this.organizationId(), this.titleId(), version.id),
      ]);
      this.attributeCount.set(attributes.length);
      this.tagCount.set(tags.length);
      this.cardTypeCount.set(cardTypes.length);
    } catch (error) {
      console.error('Failed to load title version overview.', error);
    } finally {
      this.isLoading.set(false);
    }
  }
}
