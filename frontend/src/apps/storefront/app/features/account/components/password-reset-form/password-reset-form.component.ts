import { Component, input, output, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
@Component({
  selector: 'sd-password-reset-form',
  imports: [FormsModule],
  templateUrl: './password-reset-form.component.html',
  styleUrl: './password-reset-form.component.scss',
})
export class PasswordResetForm {
  readonly busy = input(false);
  readonly submitted = output<string>();
  readonly error = signal('');
  password = '';
  confirmation = '';
  submit(form: NgForm) {
    if (this.busy()) return;
    this.error.set('');
    if (
      form.invalid ||
      this.password.length < 12 ||
      this.password.length > 128 ||
      !/[A-Z]/.test(this.password) ||
      !/[a-z]/.test(this.password) ||
      !/[0-9]/.test(this.password) ||
      !/[^a-zA-Z0-9]/.test(this.password)
    ) {
      this.error.set(
        'Folosește 12–128 de caractere, cu literă mare, literă mică, cifră și simbol.',
      );
      return;
    }
    if (this.password !== this.confirmation) {
      this.error.set('Parolele nu coincid.');
      return;
    }
    const password = this.password;
    this.password = '';
    this.confirmation = '';
    form.form.markAsPristine();
    this.submitted.emit(password);
  }
}
