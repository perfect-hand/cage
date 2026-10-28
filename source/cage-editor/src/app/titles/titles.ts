import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ConfirmDeleteDialog } from '../confirm-delete-dialog';
import { OrganizationBreadcrumb } from '../organizations/organization-breadcrumb';
import { Title, TitlesService } from './titles-service';

@Component({
  selector: 'titles',
  imports: [
    MatProgressSpinnerModule,
    MatButtonModule,
    MatIconModule,
    RouterLink,
    OrganizationBreadcrumb,
  ],
  templateUrl: './titles.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './titles.scss',
})
export class Titles implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly titlesService = inject(TitlesService);

  readonly organizationId = input.required<string>();
  readonly titles = signal<Title[]>([]);
  readonly isLoading = signal(false);
  readonly isDeleting = signal(false);

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

  async deleteTitle(titleId: string, name: string): Promise<void> {
    if (this.isDeleting()) {
      return;
    }

    const confirmed = await firstValueFrom(
      this.dialog
        .open(ConfirmDeleteDialog, {
          data: {
            name,
            consequence: 'All attributes, tags, cards and triggers of that title will be lost.',
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
