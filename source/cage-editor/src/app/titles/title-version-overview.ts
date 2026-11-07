import { DatePipe } from '@angular/common';
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
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AttributeDto } from '../attributes/attributes-model';
import { TagDto } from '../tags/tags-model';
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
  private readonly http = inject(HttpClient);
  private readonly versionsService = inject(TitleVersionsService);

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

      const baseUrl = `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${version.id}`;
      const [attributes, tags] = await Promise.all([
        firstValueFrom(this.http.get<AttributeDto[]>(`${baseUrl}/attributes`)),
        firstValueFrom(this.http.get<TagDto[]>(`${baseUrl}/tags`)),
      ]);
      this.attributeCount.set(attributes?.length ?? 0);
      this.tagCount.set(tags?.length ?? 0);
    } catch (error) {
      console.error('Failed to load title version overview.', error);
    } finally {
      this.isLoading.set(false);
    }
  }
}
