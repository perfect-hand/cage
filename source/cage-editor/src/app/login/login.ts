import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
} from '@angular/core';
import { AuthService } from '../auth-service';

@Component({
  selector: 'login',
  imports: [],
  templateUrl: './login.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './login.scss',
})
export class Login implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);

  readonly isSignedIn = this.authService.isSignedIn;
  readonly userName = this.authService.userName;

  ngOnInit(): void {
    void this.authService.initialize();
  }

  signIn(): Promise<void> {
    return this.authService.signIn();
  }

  signOut(): Promise<void> {
    return this.authService.signOut();
  }

  async sendRequest(): Promise<void> {
    const token = this.authService.getAccessToken();

    if (!token) {
      return;
    }

    this.http
      .get('http://localhost:5267/organizations', {
        headers: {
          Authorization: `Bearer ${token}`,
        },
        responseType: 'text',
      })
      .subscribe((response) => {
        console.log(response);
      });
  }
}
