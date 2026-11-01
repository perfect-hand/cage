import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ConfirmDeleteDialog } from '../confirm-delete-dialog';
import { maximumTitleVersions, TitleVersion, TitleVersionsService } from './title-versions-service';

@Component({
  selector: 'title-versions',
  imports: [FormsModule, MatButtonModule, MatProgressSpinnerModule, RouterLink],
  templateUrl: './title-versions.html',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class TitleVersions implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly versionsService = inject(TitleVersionsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versions = signal<TitleVersion[]>([]);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly newVersionName = signal('');
  readonly parentVersionId = signal('');
  readonly maximumVersions = maximumTitleVersions;

  ngOnInit(): void {
    void this.loadVersions();
  }

  private async loadVersions(): Promise<void> {
    try {
      const versions =
        (await firstValueFrom(
          this.http.get<TitleVersion[]>(
            `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions`,
          ),
        )) ?? [];
      this.versions.set(versions);
      this.versionsService.setVersions(this.titleId(), versions);
      this.parentVersionId.set(versions.find((version) => version.id === this.titleId())?.id ?? versions[0]?.id ?? '');
    } catch (error) {
      console.error('Failed to load title versions.', error);
    } finally {
      this.isLoading.set(false);
    }
  }

  async createVersion(): Promise<void> {
    const name = this.newVersionName().trim();
    if (!name || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    try {
      const version = await firstValueFrom(
        this.http.post<TitleVersion>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions`,
          { name, parentVersionId: this.parentVersionId() || null },
        ),
      );
      const versions = [...this.versions(), version];
      this.updateVersions(versions);
      await this.router.navigate([
        '/organizations',
        this.organizationId(),
        'titles',
        this.titleId(),
        'versions',
        version.id,
        'attributes',
      ]);
    } catch (error) {
      console.error('Failed to create title version.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }

  async renameVersion(version: TitleVersion, name: string): Promise<void> {
    const trimmedName = name.trim();
    if (!trimmedName || trimmedName === version.name || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    try {
      await firstValueFrom(
        this.http.put(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${version.id}`,
          { name: trimmedName },
        ),
      );
      this.updateVersions(
        this.versions().map((item) =>
          item.id === version.id ? { ...item, name: trimmedName } : item,
        ),
      );
    } catch (error) {
      console.error('Failed to rename title version.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }

  async setReadOnly(version: TitleVersion): Promise<void> {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    try {
      const isReadOnly = !version.isReadOnly;
      await firstValueFrom(
        this.http.put(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${version.id}/read-only`,
          { isReadOnly },
        ),
      );
      this.updateVersions(
        this.versions().map((item) =>
          item.id === version.id ? { ...item, isReadOnly } : item,
        ),
      );
    } catch (error) {
      console.error('Failed to update title version.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }

  async deleteVersion(version: TitleVersion): Promise<void> {
    if (this.isSubmitting()) {
      return;
    }

    const confirmed = await firstValueFrom(
      this.dialog
        .open(ConfirmDeleteDialog, {
          data: { name: version.name, requireNameConfirmation: true },
        })
        .afterClosed(),
    );
    if (!confirmed) {
      return;
    }

    this.isSubmitting.set(true);
    try {
      await firstValueFrom(
        this.http.delete(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${version.id}`,
        ),
      );
      const versions = this.versions().filter((item) => item.id !== version.id);
      this.updateVersions(versions);
      if (this.parentVersionId() === version.id) {
        this.parentVersionId.set(versions[0]?.id ?? '');
      }
    } catch (error) {
      console.error('Failed to delete title version.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private updateVersions(versions: TitleVersion[]): void {
    this.versions.set(versions);
    this.versionsService.setVersions(this.titleId(), versions);
  }
}
