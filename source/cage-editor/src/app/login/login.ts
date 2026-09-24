import { HttpClient } from '@angular/common/http';
import {
  Component,
  inject,
  ChangeDetectionStrategy,
  OnInit,
} from '@angular/core';
import {AuthService} from '../auth-service';

@Component({
  selector: 'login',
  imports: [],
  templateUrl: './login.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './login.scss',
})
export class Login implements OnInit {
  
  private http = inject(HttpClient);
  protected authService = inject(AuthService);

  ngOnInit() {
    this.authService.initialize();
  }

  protected async sendRequest() {
    this.http
          .get('http://localhost:5267/organizations', {
            headers: {
              Authorization: 'Bearer ' + this.authService.accessToken,
            },
            responseType: 'text',
          })
          .subscribe((response) => {
            console.log(response);
          });
  }
}
