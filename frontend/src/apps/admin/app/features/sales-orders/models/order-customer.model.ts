import { OrderAddress } from './order-address.model';
export interface OrderCustomer {
  customerType: 'person' | 'company';
  fullName: string;
  email: string;
  phone: string;
  companyName: string | null;
  taxId: string | null;
  registrationNumber: string | null;
  shippingAddress: OrderAddress;
  billingSameAsShipping: boolean;
  billingAddress: OrderAddress | null;
}
