import { CustomerAddress } from './customer-address.model';
export interface CustomerDraft {
  customerType: 'person' | 'company';
  fullName: string;
  email: string;
  phone: string;
  companyName: string | null;
  taxId: string | null;
  registrationNumber: string | null;
  shippingAddress: CustomerAddress;
  billingSameAsShipping: boolean;
  billingAddress: CustomerAddress | null;
}
