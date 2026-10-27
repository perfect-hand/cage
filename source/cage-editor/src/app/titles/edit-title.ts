import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { OrganizationBreadcrumb } from '../organizations/organization-breadcrumb';
import { Title, TitlesService } from './titles-service';

@Component({
  selector: 'edit-title',
  imports: [FormField, MatButtonModule, MatProgressSpinnerModule, RouterLink, OrganizationBreadcrumb],
  templateUrl: './edit-title.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './edit-title.scss',
})
export class EditTitle implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly titlesService = inject(TitlesService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly notFound = signal(false);
  readonly name = signal('');
  readonly nameForm = form(this.name, (path) => {
    required(path, { message: 'Enter a title name.' });
    pattern(path, /\S/, { message: 'Enter a title name.' });
    maxLength(path, 50, { message: 'Enter a title name of at most 50 characters.' });
  });

  ngOnInit(): void {
    void this.loadTitle();
  }

  private async loadTitle(): Promise<void> {
    let titles = this.titlesService.getTitles(this.organizationId());
    let title = titles.find((item) => item.id === this.titleId());

    if (!title) {
      try {
        titles = await firstValueFrom(
          this.http.get<Title[]>(`/organizations/${this.organizationId()}/titles`),
        ) ?? [];
        this.titlesService.setTitles(this.organizationId(), titles);
        title = titles.find((item) => item.id === this.titleId());
      } catch (error) {
        console.error('Failed to load titles.', error);
      }
    }

    if (title) {
      this.name.set(title.name);
    } else {
      this.notFound.set(true);
    }
    this.isLoading.set(false);
  }

  async updateTitle(): Promise<void> {
    if (this.nameForm().invalid() || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);

    try {
      const name = this.name().trim();
      await firstValueFrom(
        this.http.put(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}`,
          { name },
        ),
      );
      const titles = this.titlesService
        .getTitles(this.organizationId())
        .map((title) => title.id === this.titleId() ? { ...title, name } : title);
      this.titlesService.setTitles(this.organizationId(), titles);
      await this.router.navigate(['/organizations', this.organizationId(), 'titles']);
    } catch (error) {
      console.error('Failed to update title.', error);
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
