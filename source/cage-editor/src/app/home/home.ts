import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { firstValueFrom, filter } from 'rxjs';

import { AuthService } from '../auth-service';
import { Organization, OrganizationService } from '../organizations/organization-service';
import { Title, TitlesService } from '../titles/titles-service';

@Component({
  selector: 'home',
  imports: [MatButtonModule, MatIconModule, RouterLink, RouterOutlet],
  templateUrl: './home.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './home.scss',
})
export class Home {
  private readonly authService = inject(AuthService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly organizationService = inject(OrganizationService);
  private readonly titlesService = inject(TitlesService);
  private readonly url = signal(this.router.url);
  private readonly requested = new Set<string>();

  readonly userName = this.authService.userName;
  readonly organizationId = computed(
    () => /^\/organizations\/([^/?#]+)/.exec(this.url())?.[1] ?? undefined,
  );
  readonly titleId = computed(
    () => /^\/organizations\/[^/?#]+\/titles\/([^/?#]+)/.exec(this.url())?.[1] ?? undefined,
  );
  readonly organizationName = computed(() => {
    const id = this.organizationId();
    return this.organizationService.organizations().find((o) => o.id === id)?.name ?? '…';
  });
  readonly titleName = computed(() => {
    const organizationId = this.organizationId();
    const titleId = this.titleId();
    if (!organizationId || titleId === 'new') {
      return undefined;
    }
    return (
      this.titlesService.getTitles(organizationId).find((t) => t.id === titleId)?.name ?? '…'
    );
  });
  readonly activeTitleId = computed(() => (this.titleId() === 'new' ? undefined : this.titleId()));

  constructor() {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => this.url.set(event.urlAfterRedirects));

    effect(() => {
      const organizationId = this.organizationId();
      if (organizationId && this.organizationName() === '…' && !this.requested.has('organizations')) {
        this.requested.add('organizations');
        void this.loadOrganizations();
      }
    });
    effect(() => {
      const organizationId = this.organizationId();
      const titleId = this.activeTitleId();
      if (organizationId && titleId && this.titleName() === '…' && !this.requested.has(organizationId)) {
        this.requested.add(organizationId);
        void this.loadTitles(organizationId);
      }
    });
  }

  signOut(): Promise<void> {
    return this.authService.signOut();
  }

  private async loadOrganizations(): Promise<void> {
    try {
      const response = await firstValueFrom(this.http.get<Organization[]>('/organizations'));
      this.organizationService.setOrganizations(response ?? []);
    } catch (error) {
      console.error('Failed to load organizations.', error);
    }
  }

  private async loadTitles(organizationId: string): Promise<void> {
    try {
      const response = await firstValueFrom(
        this.http.get<Title[]>(`/organizations/${organizationId}/titles`),
      );
      this.titlesService.setTitles(organizationId, response ?? []);
    } catch (error) {
      console.error('Failed to load titles.', error);
    }
  }
}
