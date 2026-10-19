import { Component, input, output, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Variant } from '@shared/models';
import { ImageUploadRequest } from '../../models/image-upload.request';
@Component({
  selector: 'sd-image-upload-form',
  imports: [FormsModule],
  templateUrl: './image-upload-form.component.html',
  styleUrl: './image-upload-form.component.scss',
})
export class ImageUploadForm {
  readonly variants = input.required<Variant[]>();
  readonly disabled = input(false);
  readonly submitted = output<ImageUploadRequest>();
  readonly file = signal<File | null>(null);
  readonly error = signal('');
  altText = '';
  variantId = '';
  choose(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.error.set('');
    this.file.set(null);
    if (file && file.size > 8 * 1024 * 1024) {
      this.error.set('Fișierul depășește 8 MB.');
      return;
    }
    this.file.set(file);
  }
  submit(form: NgForm) {
    const file = this.file();
    if (this.disabled() || form.invalid || !file) return;
    this.submitted.emit({ file, altText: this.altText.trim(), variantId: this.variantId || null });
  }
}
