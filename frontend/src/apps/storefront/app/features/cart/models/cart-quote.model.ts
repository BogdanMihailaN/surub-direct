import { CartQuoteLine } from './cart-quote-line.model';
export interface CartQuote {
  lines: CartQuoteLine[];
  currency: string | null;
  netTotal: number | null;
  grossTotal: number | null;
  checkedAt: string;
  checkoutEnabled: false;
}
