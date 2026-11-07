import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { MatIconRegistry } from '@angular/material/icon';
import { Title } from '@angular/platform-browser';
import { RouterLink, RouterOutlet } from '@angular/router';

import { AuthService } from './auth-service';
import { environment } from '../environments/environment';
import { version } from '../environments/version';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected readonly title = signal('cage-editor');
  private readonly authService = inject(AuthService);
  private readonly matIconRegistry = inject(MatIconRegistry);
  private readonly browserTitle = inject(Title);

  readonly environmentLabel = environment.label;
  readonly version = version;

  ngOnInit(): void {
    this.matIconRegistry.setDefaultFontSetClass('material-symbols-outlined');
    this.browserTitle.setTitle(`CaGE Editor (${environment.label})`);
    void this.authService.initialize();
  }
}
