import { CustomerDraft } from './customer-draft.model';
export interface PreparedCart {
  id: string;
  status: 'Confirming' | 'Pending' | 'Active' | 'Cancelling' | 'Released' | 'Expired' | 'Consumed';
  cancellationRequested: boolean;
  confirmationRequested: boolean;
  orderNumber: string | null;
  checkedAt: string;
  deadline: string;
  remoteExpiresAt: string | null;
  currency: string;
  netTotal: number;
  grossTotal: number;
  customer: CustomerDraft;
  lines: {
    offerId: string;
    productName: string;
    sku: string;
    offerLabel: string;
    packs: number;
    unitGross: number;
    netTotal: number;
    grossTotal: number;
  }[];
}
