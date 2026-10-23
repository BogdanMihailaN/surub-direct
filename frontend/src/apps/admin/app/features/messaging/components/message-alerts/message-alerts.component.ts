import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MessagingAlert } from '../../models/messaging-alert.model';
@Component({
  selector: 'sd-message-alerts',
  imports: [DatePipe],
  templateUrl: './message-alerts.component.html',
  styleUrl: './message-alerts.component.scss',
})
export class MessageAlerts {
  readonly alerts = input.required<MessagingAlert[]>();
  readonly observedAt = input.required<number>();
}
