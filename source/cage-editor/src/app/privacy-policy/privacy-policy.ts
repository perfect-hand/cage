import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'privacy-policy',
  imports: [RouterLink],
  templateUrl: './privacy-policy.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './privacy-policy.scss',
})
export class PrivacyPolicy {}

