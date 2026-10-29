import { Component, input, output } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
@Component({
  selector: 'sd-account-form',
  imports: [FormsModule],
  templateUrl: './account-form.component.html',
  styleUrl: './account-form.component.scss',
})
export class AccountForm {
  readonly register = input(false);
  readonly busy = input(false);
  readonly credentials = output<{ email: string; password: string }>();
  email = '';
  password = '';
  confirmation = '';
  submit(form: NgForm) {
    if (this.busy() || form.invalid || (this.register() && this.password !== this.confirmation))
      return;
    this.credentials.emit({ email: this.email, password: this.password });
    this.password = '';
    this.confirmation = '';
    form.form.get('password')?.markAsUntouched();
    form.form.get('confirmation')?.markAsUntouched();
  }
}
