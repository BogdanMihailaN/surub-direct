import { Component, input, output, signal, effect } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { ImportPreview as Preview } from '../../models/import-preview.model';
@Component({
  selector: 'sd-import-preview',
  imports: [DecimalPipe, MatButtonModule],
  templateUrl: './import-preview.component.html',
  styleUrl: './import-preview.component.scss',
})
export class ImportPreview {
  readonly preview = input.required<Preview>();
  readonly busy = input(false);
  readonly save = output<void>();
  readonly confirmed = signal(false);
  constructor() {
    effect(() => {
      this.preview();
      this.confirmed.set(false);
    });
  }
  confirm(event: Event) {
    this.confirmed.set((event.target as HTMLInputElement).checked);
  }
}
