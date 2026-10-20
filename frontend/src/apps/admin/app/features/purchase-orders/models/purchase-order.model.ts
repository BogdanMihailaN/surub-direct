import { PurchaseOrderLine } from './purchase-order-line.model';
export interface PurchaseOrder {
  id: string;
  number: number;
  supplierId: string;
  supplierName: string;
  supplierCountry: string;
  supplierTaxId: string;
  supplierEmail: string;
  supplierAddress: string;
  currency: string;
  notes: string;
  status: 'Draft' | 'Sent' | 'Cancelled';
  version: number;
  createdAt: string;
  sentAt: string | null;
  cancelledAt: string | null;
  cancellationReason: string;
  netTotal: number;
  lines: PurchaseOrderLine[];
}
