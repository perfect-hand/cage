import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../auth-service';

interface Organization {
  name: string;
}

@Component({
  selector: 'organizations',
  imports: [MatProgressSpinnerModule, MatButtonModule, RouterLink],
  templateUrl: './organizations.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './organizations.scss',
})
export class Organizations implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  readonly organizations = signal<Organization[]>([]);
  readonly isLoading = signal(false);

    ngOnInit(): void {
        void this.loadOrganizations();
    }

  private async loadOrganizations(): Promise<void> {
    this.isLoading.set(true);

    try {
      const token = await this.authService.getAccessToken();
      const response = await firstValueFrom(
        this.http.get<Organization[]>('http://localhost:5267/organizations', {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }),
      );

      this.organizations.set(response ?? []);
    } catch (error) {
      console.error('Failed to load organizations.', error);
      this.organizations.set([]);
    } finally {
      this.isLoading.set(false);
    }
  }
}
