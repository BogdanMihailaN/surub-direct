import {
  afterNextRender,
  Component,
  effect,
  ElementRef,
  inject,
  Injector,
  input,
  viewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { CartSelection } from '../../../cart/models/cart-selection.model';
import { AccountOfferStore } from '../../state/account-offer.store';
@Component({
  selector: 'sd-add-to-account',
  imports: [RouterLink],
  providers: [AccountOfferStore],
  templateUrl: './add-to-account.component.html',
  styleUrl: './add-to-account.component.scss',
})
export class AddToAccount {
  readonly selection = input<CartSelection | null>(null);
  readonly state = inject(AccountOfferStore);
  private readonly injector = inject(Injector);
  private readonly confirmation = viewChild<ElementRef<HTMLElement>>('confirmation');
  constructor() {
    effect(() => {
      this.selection();
      this.state.invalidate();
    });
  }
  async select() {
    const line = this.selection();
    if (!line) return;
    await this.state.select(line);
    afterNextRender(() => this.confirmation()?.nativeElement.focus(), { injector: this.injector });
  }
  existing() {
    const s = this.state.selected();
    return s?.cart?.lines.find((x) => x.offerId === s.line.offerId)?.packs;
  }
}
