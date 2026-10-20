import { PurchaseReceiptCorrection } from './purchase-receipt-correction.model';
import { PurchaseReceipt } from './purchase-receipt.model';
import { PurchaseOrderLine } from './purchase-order-line.model';
export interface PurchaseOrder {
  receipts: PurchaseReceipt[];
  corrections: PurchaseReceiptCorrection[];
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
