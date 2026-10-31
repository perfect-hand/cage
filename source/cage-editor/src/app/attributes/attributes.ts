import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ConfirmDeleteDialog } from '../confirm-delete-dialog';
import { Attribute, AttributesService } from './attributes-service';

export const maximumAttributes = 100;

@Component({
  selector: 'attributes',
  imports: [MatProgressSpinnerModule, MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './attributes.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './attributes.scss',
})
export class Attributes implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);
  private readonly attributesService = inject(AttributesService);

  readonly organizationId = input.required<string>();
  readonly titleId = input.required<string>();
  readonly attributes = signal<Attribute[]>([]);
  readonly isLoading = signal(false);
  readonly isDeleting = signal(false);
  readonly maximumAttributes = maximumAttributes;

  ngOnInit(): void {
    void this.loadAttributes();
  }

  private async loadAttributes(): Promise<void> {
    this.isLoading.set(true);

    try {
      const response =
        (await firstValueFrom(
          this.http.get<Attribute[]>(
            `/organizations/${this.organizationId()}/titles/${this.titleId()}/attributes`,
          ),
        )) ?? [];

      this.attributes.set(response);
      this.attributesService.setAttributes(this.titleId(), response);
    } catch (error) {
      console.error('Failed to load attributes.', error);
      this.attributes.set([]);
      this.attributesService.setAttributes(this.titleId(), []);
    } finally {
      this.isLoading.set(false);
    }
  }

  async deleteAttribute(attributeId: string, name: string): Promise<void> {
    if (this.isDeleting()) {
      return;
    }

    const confirmed = await firstValueFrom(
      this.dialog
        .open(ConfirmDeleteDialog, {
          data: {
            name,
            requireNameConfirmation: false,
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
        this.http.delete(
          `/organizations/${this.organizationId()}/titles/${this.titleId()}/attributes/${attributeId}`,
        ),
      );
      const attributes = this.attributes().filter((attribute) => attribute.id !== attributeId);
      this.attributes.set(attributes);
      this.attributesService.setAttributes(this.titleId(), attributes);
    } catch (error) {
      console.error('Failed to delete attribute.', error);
    } finally {
      this.isDeleting.set(false);
    }
  }
}
