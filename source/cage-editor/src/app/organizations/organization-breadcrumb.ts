import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrganizationService } from './organization-service';

@Component({
  selector: 'organization-breadcrumb',
  imports: [RouterLink],
  templateUrl: './organization-breadcrumb.html',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class OrganizationBreadcrumb {
  private readonly organizationService = inject(OrganizationService);

  readonly organizationId = input.required<string>();
  readonly current = input<string>();
  readonly organizationName = computed(
    () =>
      this.organizationService.organizations().find((o) => o.id === this.organizationId())?.name ??
      'Unknown organization',
  );
}
