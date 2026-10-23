import { Component, inject, afterNextRender } from '@angular/core';
import { MessagingStore } from '../../state/messaging.store';
import { MessageAlerts } from '../../components/message-alerts/message-alerts.component';
import { MessageMetrics } from '../../components/message-metrics/message-metrics.component';
import { FailedMessageList } from '../../components/failed-message-list/failed-message-list.component';
import { MessageReplay } from '../../components/message-replay/message-replay.component';
@Component({
  selector: 'sd-messaging',
  imports: [MessageAlerts, MessageMetrics, FailedMessageList, MessageReplay],
  providers: [MessagingStore],
  templateUrl: './messaging.component.html',
  styleUrl: './messaging.component.scss',
})
export class MessagingPage {
  readonly state = inject(MessagingStore);
  constructor() {
    afterNextRender(() => void this.state.load());
  }
}
