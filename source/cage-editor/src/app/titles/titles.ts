import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { OrganizationBreadcrumb } from '../organizations/organization-breadcrumb';

interface Title {
  id: string;
  name: string;
}

@Component({
  selector: 'titles',
  imports: [MatProgressSpinnerModule, MatButtonModule, RouterLink, OrganizationBreadcrumb],
  templateUrl: './titles.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './titles.scss',
})
export class Titles implements OnInit {
  private readonly http = inject(HttpClient);

  readonly organizationId = input.required<string>();
  readonly titles = signal<Title[]>([]);
  readonly isLoading = signal(false);

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
    } catch (error) {
      console.error('Failed to load titles.', error);
      this.titles.set([]);
    } finally {
      this.isLoading.set(false);
    }
  }
}
