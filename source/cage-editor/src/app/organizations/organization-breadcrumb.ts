import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

interface Organization {
  id: string;
  name: string;
}

@Component({
  selector: 'organization-breadcrumb',
  imports: [RouterLink],
  template: `
    <nav class="breadcrumb" aria-label="Breadcrumb">
      <ol>
        <li><a routerLink="/">Organizations</a></li>
        <li>
          @if (current()) {
          <a [routerLink]="['/organizations', organizationId(), 'titles']">{{ organizationName() }}</a>
          } @else {
          <span aria-current="page">{{ organizationName() }}</span>
          }
        </li>
        @if (current()) {
        <li><span aria-current="page">{{ current() }}</span></li>
        }
      </ol>
    </nav>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class OrganizationBreadcrumb implements OnInit {
  private readonly http = inject(HttpClient);

  readonly organizationId = input.required<string>();
  readonly current = input<string>();
  readonly organizationName = signal('…');

  ngOnInit(): void {
    void this.loadOrganizationName();
  }

  private async loadOrganizationName(): Promise<void> {
    try {
      const organizations = await firstValueFrom(this.http.get<Organization[]>('/organizations'));
      const organization = (organizations ?? []).find((o) => o.id === this.organizationId());

      this.organizationName.set(organization?.name ?? 'Unknown organization');
    } catch (error) {
      console.error('Failed to load organization.', error);
      this.organizationName.set('Unknown organization');
    }
  }
}
