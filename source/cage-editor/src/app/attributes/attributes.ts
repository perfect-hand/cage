import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AttributeDto } from './attributes-model';
import { ConfirmDeleteDialog } from '../confirm-delete-dialog';
import { TitleVersionsService } from '../titles/title-versions-service';
import { AttributesService } from './attributes-service';

export const maximumAttributes = 100;

@Component({
  selector: 'attributes',
  imports: [MatProgressSpinnerModule, MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './attributes.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './attributes.scss',
})
export class Attributes implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly attributesService = inject(AttributesService);
  private readonly versionsService = inject(TitleVersionsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly attributes = signal<AttributeDto[]>([]);
  readonly searchTerm = signal('');
  readonly filteredAttributes = computed(() => {
    const searchTerm = this.searchTerm().trim().toLowerCase();
    return this.attributes().filter(
      (attribute) =>
        attribute.name.toLowerCase().includes(searchTerm) || attribute.type.toLowerCase().includes(searchTerm),
    );
  });
  readonly isLoading = signal(false);
  readonly isDeleting = signal(false);
  readonly isReadOnly = signal(false);
  readonly notFound = signal(false);
  readonly maximumAttributes = maximumAttributes;

  ngOnInit(): void {
    void this.loadAttributes();
  }

  setSearchTerm(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  private async loadAttributes(): Promise<void> {
    this.isLoading.set(true);
    const versionId = this.versionId();

    try {
      const versions = await this.versionsService.loadVersions(this.organizationId(), this.titleId());

      const version = versions.find((item) => item.id === versionId);
      if (!version) {
        this.notFound.set(true);
        return;
      }
      this.isReadOnly.set(version.isReadOnly);

      const attributes = await this.attributesService.loadAttributes(
        this.organizationId(),
        this.titleId(),
        versionId,
      );
      this.attributes.set(attributes);
    } catch (error) {
      console.error('Failed to load attributes.', error);
      this.attributes.set([]);
    } finally {
      this.isLoading.set(false);
    }
  }

  async deleteAttribute(attributeId: string, name: string): Promise<void> {
    if (this.isDeleting() || this.isReadOnly()) {
      return;
    }

    const versionId = this.versionId();
    const confirmed = await firstValueFrom(
      this.dialog
        .open(ConfirmDeleteDialog, {
          data: {
            name,
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
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${versionId}/attributes/${attributeId}`,
        ),
      );
      const attributes = this.attributes().filter((attribute) => attribute.id !== attributeId);
      this.attributes.set(attributes);
      this.attributesService.setAttributes(this.titleId(), versionId, attributes);
    } catch (error) {
      console.error('Failed to delete attribute.', error);
    } finally {
      this.isDeleting.set(false);
    }
  }
}
