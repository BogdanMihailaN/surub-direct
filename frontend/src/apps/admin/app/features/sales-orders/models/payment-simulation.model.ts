export interface PaymentSimulation {
  status: string;
  version: number;
  attemptId: string | null;
  amount: number;
  refundedAmount: number;
  currency: string;
  history: {
    operationId: string;
    attemptId: string;
    version: number;
    action: string;
    partialReturnId: string | null;
    refundAmount: number | null;
    fromStatus: string;
    toStatus: string;
    reason: string;
    actor: string;
    createdAt: string;
  }[];
}
