export interface PaymentSimulation {
  status: string;
  version: number;
  attemptId: string | null;
  amount: number;
  currency: string;
  history: {
    operationId: string;
    attemptId: string;
    version: number;
    action: string;
    fromStatus: string;
    toStatus: string;
    reason: string;
    actor: string;
    createdAt: string;
  }[];
}
