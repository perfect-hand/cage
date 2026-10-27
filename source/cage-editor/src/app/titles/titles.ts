import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { form, FormField, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { OrganizationBreadcrumb } from '../organizations/organization-breadcrumb';
import { Title, TitlesService } from './titles-service';

@Component({
  selector: 'titles',
  imports: [FormField, MatProgressSpinnerModule, MatButtonModule, RouterLink, OrganizationBreadcrumb],
  templateUrl: './titles.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './titles.scss',
})
export class Titles implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly titlesService = inject(TitlesService);

  readonly organizationId = input.required<string>();
  readonly titles = signal<Title[]>([]);
  readonly isLoading = signal(false);
  readonly editingTitleId = signal<string | null>(null);
  readonly isSaving = signal(false);
  readonly isDeleting = signal(false);
  readonly name = signal('');
  readonly nameForm = form(this.name, (path) => {
    required(path, { message: 'Enter a title name.' });
    pattern(path, /\S/, { message: 'Enter a title name.' });
    maxLength(path, 50, { message: 'Enter a title name of at most 50 characters.' });
  });

  ngOnInit(): void {
    void this.loadTitles();
  }

  private async loadTitles(): Promise<void> {
    this.isLoading.set(true);

    try {
      const response = await firstValueFrom(
        this.http.get<Title[]>(`/organizations/${this.organizationId()}/titles`),
      );

      this.titles.set(response ?? []);
      this.titlesService.setTitles(this.organizationId(), response ?? []);
    } catch (error) {
      console.error('Failed to load titles.', error);
      this.titles.set([]);
      this.titlesService.setTitles(this.organizationId(), []);
    } finally {
      this.isLoading.set(false);
    }
  }

  startEditing(titleId: string, name: string): void {
    this.name.set(name);
    this.editingTitleId.set(titleId);
  }

  cancelEditing(): void {
    this.editingTitleId.set(null);
  }

  async updateTitle(titleId: string): Promise<void> {
    if (this.nameForm().invalid() || this.isSaving()) {
      return;
    }

    this.isSaving.set(true);

    try {
      const name = this.name().trim();
      await firstValueFrom(
        this.http.put(`/organizations/${this.organizationId()}/titles/${titleId}`, { name }),
      );
      const titles = this.titles().map((title) =>
        title.id === titleId ? { ...title, name } : title,
      );
      this.titles.set(titles);
      this.titlesService.setTitles(this.organizationId(), titles);
      this.editingTitleId.set(null);
    } catch (error) {
      console.error('Failed to update title.', error);
    } finally {
      this.isSaving.set(false);
    }
  }

  async deleteTitle(titleId: string, name: string): Promise<void> {
    if (this.isDeleting() || !window.confirm(`Delete "${name}"?`)) {
      return;
    }

    this.isDeleting.set(true);

    try {
      await firstValueFrom(
        this.http.delete(`/organizations/${this.organizationId()}/titles/${titleId}`),
      );
      const titles = this.titles().filter((title) => title.id !== titleId);
      this.titles.set(titles);
      this.titlesService.setTitles(this.organizationId(), titles);
    } catch (error) {
      console.error('Failed to delete title.', error);
    } finally {
      this.isDeleting.set(false);
    }
  }
}
