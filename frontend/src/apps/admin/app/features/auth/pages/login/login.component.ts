import { Component, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { SessionService } from '../../../../core/auth/session.service';
import { errorMessage } from '@shared/http/error-message';
@Component({
  selector: 'sd-login',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  readonly session = inject(SessionService);
  private readonly router = inject(Router);
  readonly busy = signal(false);
  readonly error = signal('');
  email = 'admin@surubdirect.local';
  password = '';
  async login(form: NgForm) {
    if (form.invalid || this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    try {
      await this.session.login(this.email, this.password);
      this.password = '';
      await this.router.navigate(['/catalog']);
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.busy.set(false);
    }
  }
}
