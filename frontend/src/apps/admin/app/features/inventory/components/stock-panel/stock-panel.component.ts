import { Component, input, output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StockLedgerStore } from '../../state/stock-ledger.store';
import { AdjustmentForm } from '../adjustment-form/adjustment-form.component';
import { MovementList } from '../movement-list/movement-list.component';
@Component({
  selector: 'sd-stock-panel',
  imports: [FormsModule, AdjustmentForm, MovementList],
  providers: [StockLedgerStore],
  templateUrl: './stock-panel.component.html',
  styleUrl: './stock-panel.component.scss',
})
export class StockPanel {
  readonly variants = input.required<readonly { id: string; sku: string }[]>();
  readonly disabled = input(false);
  readonly updated = output<void>();
  readonly store = inject(StockLedgerStore);
  async adjust(c: { quantity: number; reference: string; reason: string }) {
    if (await this.store.adjust(c)) this.updated.emit();
  }
  async retry() {
    if (await this.store.retry()) this.updated.emit();
  }
}
