export interface PartialReturnAction {
  id: string;
  version: number;
  target: 'Approved' | 'Rejected' | 'Receive';
  reason: string;
  resalableConfirmed: boolean;
}
