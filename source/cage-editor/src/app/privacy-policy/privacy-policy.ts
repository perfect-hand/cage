import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'privacy-policy',
  templateUrl: './privacy-policy.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './privacy-policy.scss',
})
export class PrivacyPolicy implements OnInit {
  private readonly browserTitle = inject(Title);

  ngOnInit(): void {
    this.browserTitle.setTitle('Privacy policy · CaGE');
  }
}
