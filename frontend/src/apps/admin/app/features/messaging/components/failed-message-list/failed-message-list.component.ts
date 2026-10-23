import { Component, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MessagingStatus } from '../../models/messaging-status.model';
@Component({
  selector: 'sd-failed-message-list',
  imports: [DatePipe],
  templateUrl: './failed-message-list.component.html',
  styleUrl: './failed-message-list.component.scss',
})
export class FailedMessageList {
  readonly data = input.required<MessagingStatus>();
  readonly disabled = input(false);
  readonly selected = input<string | null>(null);
  readonly chosen = output<string>();
  readonly paged = output<number>();
}
