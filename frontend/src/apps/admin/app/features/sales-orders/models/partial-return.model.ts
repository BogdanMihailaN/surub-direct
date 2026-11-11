export interface PartialReturn {
  id: string;
  orderId: string;
  status: string;
  version: number;
  reason: string;
  actor: string;
  createdAt: string;
  lines: { offerId: string; variantId: string; productName: string; sku: string; offerLabel: string; packs: number; unitsPerPack: number; quantity: number }[];
  decisionReason: string | null;
  decidedBy: string | null;
  decidedAt: string | null;
  receiptReason: string | null;
  receivedBy: string | null;
  receiptRequestedAt: string | null;
  inventoryReceivedAt: string | null;
  receiptAttempts: number;
  nextReceiptAttemptAt: string | null;
  receiptError: string | null;
  receiptNeedsAttention: boolean;
  receiptRetries: { operationId: string; version: number; reason: string; actor: string; createdAt: string }[];
}
