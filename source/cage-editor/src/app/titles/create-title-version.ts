import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { maximumTitleVersions, TitleVersion, TitleVersionsService } from './title-versions-service';

@Component({
  selector: 'create-title-version',
  imports: [FormField, MatButtonModule, MatProgressSpinnerModule, RouterLink],
  templateUrl: './create-title-version.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './title-version-form.scss',
})
export class CreateTitleVersion implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly versionsService = inject(TitleVersionsService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly versions = signal<TitleVersion[]>([]);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly name = signal('');
  readonly parentVersionId = signal('');
  readonly nameForm = form(this.name, (path) => {
    required(path, { message: 'Enter a version name.' });
    pattern(path, /\S/, { message: 'Enter a version name.' });
    maxLength(path, 50, { message: 'Enter a version name of at most 50 characters.' });
  });
  readonly maximumVersions = maximumTitleVersions;

  ngOnInit(): void {
    void this.loadVersions();
  }

  private async loadVersions(): Promise<void> {
    try {
      const versions = await this.versionsService.loadVersions(this.organizationId(), this.titleId());
      this.versions.set(versions);
      this.parentVersionId.set(
        versions.find((version) => version.parentVersionId === null)?.id ?? versions[0]?.id ?? '',
      );
    } catch (error) {
      console.error('Failed to load title versions.', error);
    } finally {
      this.isLoading.set(false);
    }
  }

  async createVersion(): Promise<void> {
    if (
      this.nameForm().invalid() ||
      this.isSubmitting() ||
      this.versions().length >= this.maximumVersions
    ) {
      return;
    }

    this.isSubmitting.set(true);
    try {
      const version = await firstValueFrom(
        this.http.post<TitleVersion>(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/versions`,
          { name: this.name().trim(), parentVersionId: this.parentVersionId() || null },
        ),
      );
      this.versionsService.setVersions(this.titleId(), [...this.versions(), version]);
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
}
