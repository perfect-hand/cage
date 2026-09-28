import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { AuthService } from '../auth-service';

@Component({
  selector: 'landing',
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './landing.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './landing.scss',
})
export class Landing {
  private readonly authService = inject(AuthService);

  signIn(): Promise<void> {
    return this.authService.signIn();
  }
}
