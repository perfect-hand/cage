import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ConfirmDeleteDialog } from '../confirm-delete-dialog';
import { SetTitleVersionReadOnlyRequestDto, TitleVersionDto } from './title-versions-model';
import { maximumTitleVersions, TitleVersionsService } from './title-versions-service';

@Component({
  selector: 'title-versions',
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule, RouterLink],
  templateUrl: './title-versions.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './title-versions.scss',
})
export class TitleVersions implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly versionsService = inject(TitleVersionsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versions = signal<TitleVersionDto[]>([]);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly maximumVersions = maximumTitleVersions;

  ngOnInit(): void {
    void this.loadVersions();
  }

  private async loadVersions(): Promise<void> {
    try {
      const versions = await this.versionsService.loadVersions(
        this.organizationId(),
        this.titleId(),
      );
      this.versions.set(versions);
    } catch (error) {
      console.error('Failed to load title versions.', error);
    } finally {
      this.isLoading.set(false);
    }
  }

  async setReadOnly(version: TitleVersionDto): Promise<void> {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    try {
      const isReadOnly = !version.isReadOnly;
      const request: SetTitleVersionReadOnlyRequestDto = { isReadOnly };
      await firstValueFrom(
        this.http.put<void>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${version.id}/read-only`,
          request,
        ),
      );
      this.updateVersions(
        this.versions().map((item) => (item.id === version.id ? { ...item, isReadOnly } : item)),
      );
    } catch (error) {
      console.error('Failed to update title version.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }

  async deleteVersion(version: TitleVersionDto): Promise<void> {
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
        this.http.delete<void>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${version.id}`,
        ),
      );
      const versions = this.versions().filter((item) => item.id !== version.id);
      this.updateVersions(versions);
    } catch (error) {
      console.error('Failed to delete title version.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private updateVersions(versions: TitleVersionDto[]): void {
    this.versions.set(versions);
    this.versionsService.setVersions(this.titleId(), versions);
  }
}
