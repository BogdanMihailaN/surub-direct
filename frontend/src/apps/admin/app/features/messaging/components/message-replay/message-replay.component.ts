import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { FailedMessage } from '../../models/failed-message.model';
@Component({
  selector: 'sd-message-replay',
  imports: [FormsModule, DatePipe],
  templateUrl: './message-replay.component.html',
  styleUrl: './message-replay.component.scss',
})
export class MessageReplay {
  readonly data = input.required<FailedMessage>();
  readonly disabled = input(false);
  readonly replay = output<string>();
  reason = '';
  confirmed = false;
}
