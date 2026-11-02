import { afterNextRender, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { AccountRecoveryStore } from '../../state/account-recovery.store';
import { parseRecoveryLink } from '../../models/recovery-link';
import { RecoveryRequest } from '../../components/recovery-request/recovery-request.component';
import { PasswordResetForm } from '../../components/password-reset-form/password-reset-form.component';
@Component({
  selector: 'sd-account-recovery',
  imports: [RouterLink, RecoveryRequest, PasswordResetForm],
  providers: [AccountRecoveryStore],
  templateUrl: './recovery.component.html',
  styleUrl: './recovery.component.scss',
})
export class RecoveryPage {
  readonly state = inject(AccountRecoveryStore);
  readonly mode = inject(ActivatedRoute).snapshot.data['mode'] as 'request' | 'email' | 'password';
  readonly heading =
    this.mode === 'request'
      ? 'Recuperare și confirmare cont'
      : this.mode === 'email'
        ? 'Confirmă adresa de e-mail'
        : 'Resetează parola';
  constructor() {
    inject(Title).setTitle(this.heading + ' | Șurub Direct');
    afterNextRender(() => {
      if (this.mode !== 'request') {
        const fragment = window.location.hash;
        window.history.replaceState(
          window.history.state,
          '',
          window.location.pathname + window.location.search,
        );
        this.state.link.set(parseRecoveryLink(fragment));
      }
      this.state.loaded.set(true);
    });
  }
}
