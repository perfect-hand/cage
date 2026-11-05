import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { RenameTitleVersionRequestDto, TitleVersionDto } from './title-versions-model';
import { TitleVersionsService } from './title-versions-service';

@Component({
  selector: 'edit-title-version',
  imports: [FormField, MatButtonModule, MatProgressSpinnerModule, RouterLink],
  templateUrl: './edit-title-version.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './title-version-form.scss',
})
export class EditTitleVersion implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly versionsService = inject(TitleVersionsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versionId = input.required<string>();
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly notFound = signal(false);
  readonly version = signal<TitleVersionDto | undefined>(undefined);
  readonly name = signal('');
  readonly nameForm = form(this.name, (path) => {
    required(path, { message: 'Enter a version name.' });
    pattern(path, /\S/, { message: 'Enter a version name.' });
    maxLength(path, 50, { message: 'Enter a version name of at most 50 characters.' });
  });

  ngOnInit(): void {
    void this.loadVersion();
  }

  private async loadVersion(): Promise<void> {
    try {
      const versions = await this.versionsService.loadVersions(
        this.organizationId(),
        this.titleId(),
      );
      const version = versions.find((item) => item.id === this.versionId());
      if (version) {
        this.version.set(version);
        this.name.set(version.name);
      } else {
        this.notFound.set(true);
      }
    } catch (error) {
      console.error('Failed to load title versions.', error);
      this.notFound.set(true);
    } finally {
      this.isLoading.set(false);
    }
  }

  async updateVersion(): Promise<void> {
    const version = this.version();
    if (!version || this.nameForm().invalid() || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    try {
      const name = this.name().trim();
      const request: RenameTitleVersionRequestDto = { name };
      await firstValueFrom(
        this.http.put<void>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions/${version.id}`,
          request,
        ),
      );
      this.versionsService.setVersions(
        this.titleId(),
        this.versionsService
          .getVersions(this.titleId())
          .map((item) => (item.id === version.id ? { ...item, name } : item)),
      );
      await this.router.navigate([
        '/organizations',
        this.organizationId(),
        'titles',
        this.titleId(),
        'versions',
      ]);
    } catch (error) {
      console.error('Failed to rename title version.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
