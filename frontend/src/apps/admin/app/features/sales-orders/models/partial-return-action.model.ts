export interface PartialReturnAction {
  id: string;
  version: number;
  target: 'Approved' | 'Rejected' | 'Receive' | 'Refund';
  reason: string;
  resalableConfirmed: boolean;
}
