import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import {
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { filter } from 'rxjs';

import { AuthService } from '../auth-service';
import { Landing } from '../landing/landing';
import { OrganizationService } from '../organizations/organization-service';
import { TitleVersionsService } from '../titles/title-versions-service';
import { TitlesService } from '../titles/titles-service';

@Component({
  selector: 'home',
  imports: [MatButtonModule, MatIconModule, RouterLink, RouterLinkActive, RouterOutlet, Landing],
  templateUrl: './home.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './home.scss',
})
export class Home {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly organizationService = inject(OrganizationService);
  private readonly titlesService = inject(TitlesService);
  private readonly titleVersionsService = inject(TitleVersionsService);
  private readonly url = signal(this.router.url);
  private readonly requested = new Set<string>();

  readonly featureMenuOpen = signal(false);
  readonly isSignedIn = this.authService.isSignedIn;
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
  readonly activeTitleId = computed(() => {
    const titleId = this.titleId();
    const isEditingTitle = /^\/organizations\/[^/?#]+\/titles\/[^/?#]+\/edit(?:[/?#]|$)/.test(
      this.url(),
    );
    return titleId && titleId !== 'new' && !isEditingTitle ? titleId : undefined;
  });
  readonly activeVersionId = computed(
    () => {
      const versionId = /^\/organizations\/[^/?#]+\/titles\/[^/?#]+\/versions\/([^/?#]+)(?:[/?#]|$)/
        .exec(this.url())?.[1];
      return versionId === 'new' ? undefined : versionId;
    },
  );
  readonly activeVersionName = computed(() => {
    const titleId = this.activeTitleId();
    const versionId = this.activeVersionId();
    return titleId && versionId
      ? this.titleVersionsService.getVersions(titleId).find((version) => version.id === versionId)
          ?.name
      : undefined;
  });

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
    effect(() => {
      const organizationId = this.organizationId();
      const titleId = this.activeTitleId();
      const versionId = this.activeVersionId();
      const requestKey = `versions:${titleId}`;
      if (
        organizationId &&
        titleId &&
        versionId &&
        this.activeVersionName() === undefined &&
        !this.requested.has(requestKey)
      ) {
        this.requested.add(requestKey);
        void this.titleVersionsService
          .loadVersions(organizationId, titleId)
          .catch((error) => console.error('Failed to load title versions.', error));
      }
    });
  }

  signOut(): Promise<void> {
    return this.authService.signOut();
  }

  toggleFeatureMenu(): void {
    this.featureMenuOpen.update((open) => !open);
  }

  closeFeatureMenu(): void {
    this.featureMenuOpen.set(false);
  }

  private async loadOrganizations(): Promise<void> {
    try {
      await this.organizationService.loadOrganizations();
    } catch (error) {
      console.error('Failed to load organizations.', error);
    }
  }

  private async loadTitles(organizationId: string): Promise<void> {
    try {
      await this.titlesService.loadTitles(organizationId);
    } catch (error) {
      console.error('Failed to load titles.', error);
    }
  }
}
