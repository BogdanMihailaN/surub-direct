import { Component, input, output } from '@angular/core';
@Component({
  selector: 'sd-import-upload',
  templateUrl: './import-upload.component.html',
  styleUrl: './import-upload.component.scss',
})
export class ImportUpload {
  readonly busy = input(false);
  readonly selected = output<File | null>();
  reset(event: Event) {
    (event.target as HTMLInputElement).value = '';
  }
  choose(event: Event) {
    const control = event.target as HTMLInputElement;
    this.selected.emit(control.files?.[0] ?? null);
  }
}
