import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterOutlet } from '@angular/router';

import { AuthService } from './auth-service';
import { Landing } from './landing/landing';
import { environment } from '../environments/environment';
import { version } from '../environments/version';

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
  private readonly browserTitle = inject(Title);

  readonly isSignedIn = this.authService.isSignedIn;
  readonly environmentLabel = environment.label;
  readonly version = version;

  ngOnInit(): void {
    this.browserTitle.setTitle(`CaGE Editor (${environment.label})`);
    void this.authService.initialize();
  }
}
