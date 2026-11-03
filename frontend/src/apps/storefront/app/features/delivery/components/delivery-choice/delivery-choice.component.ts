import { afterNextRender, Component, inject, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DeliveryStore } from '../../state/delivery.store';
@Component({
  selector: 'sd-delivery-choice',
  imports: [DecimalPipe, FormsModule],
  providers: [DeliveryStore],
  templateUrl: './delivery-choice.component.html',
  styleUrl: './delivery-choice.component.scss',
})
export class DeliveryChoice {
  readonly state = inject(DeliveryStore);
  readonly disabled = input(false);
  readonly chosen = output<string | null>();
  readonly selected = input<string | null>(null);
  constructor() { afterNextRender(() => void this.state.load()); }
  choose(code: string) { this.chosen.emit(code || null); }
}
