import { Component, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { StockLedger } from '../../models/stock-ledger.model';
@Component({
  selector: 'sd-movement-list',
  imports: [DatePipe],
  templateUrl: './movement-list.component.html',
  styleUrl: './movement-list.component.scss',
})
export class MovementList {
  readonly ledger = input.required<StockLedger>();
  readonly disabled = input(false);
  readonly paged = output<number>();
}
