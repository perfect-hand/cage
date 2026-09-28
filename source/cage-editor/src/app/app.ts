import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AuthService } from './auth-service';
import { Landing } from './landing/landing';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Landing],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected readonly title = signal('cage-editor');
  private readonly authService = inject(AuthService);

  readonly isSignedIn = this.authService.isSignedIn;

  ngOnInit(): void {
    void this.authService.initialize();
  }
}
