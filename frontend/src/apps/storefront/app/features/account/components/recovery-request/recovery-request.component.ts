import { Component, input, output } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
@Component({
  selector: 'sd-recovery-request',
  imports: [FormsModule],
  templateUrl: './recovery-request.component.html',
  styleUrl: './recovery-request.component.scss',
})
export class RecoveryRequest {
  readonly busy = input(false);
  readonly requested = output<{ kind: 'email' | 'password'; email: string }>();
  email = '';
  kind: 'email' | 'password' = 'password';
  submit(form: NgForm) {
    if (form.invalid || this.busy()) return;
    this.requested.emit({ kind: this.kind, email: this.email.trim() });
  }
}
