import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { AuthService } from '../auth-service';
import { Organizations } from '../organizations/organizations';

@Component({
  selector: 'home',
  imports: [MatButtonModule, MatIconModule, Organizations],
  templateUrl: './home.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './home.scss',
})
export class Home {
  private readonly authService = inject(AuthService);

  readonly userName = this.authService.userName;

  signOut(): Promise<void> {
    return this.authService.signOut();
  }
}
