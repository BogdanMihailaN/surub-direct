import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MessagingStatus } from '../../models/messaging-status.model';
import { BrokerStatus } from '../../models/broker-status.model';
import { OutboxStatus } from '../../models/outbox-status.model';
@Component({
  selector: 'sd-message-metrics',
  imports: [DatePipe],
  templateUrl: './message-metrics.component.html',
  styleUrl: './message-metrics.component.scss',
})
export class MessageMetrics {
  readonly data = input<MessagingStatus | null>(null);
  readonly broker = input<BrokerStatus | null>(null);
  readonly outbox = input<OutboxStatus | null>(null);
}
