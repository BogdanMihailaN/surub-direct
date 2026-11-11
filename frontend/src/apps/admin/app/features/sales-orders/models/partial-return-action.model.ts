export interface PartialReturnAction {
  id: string;
  version: number;
  target: 'Approved' | 'Rejected' | 'Receive' | 'Refund' | 'RetryReceipt';
  reason: string;
  resalableConfirmed: boolean;
}
