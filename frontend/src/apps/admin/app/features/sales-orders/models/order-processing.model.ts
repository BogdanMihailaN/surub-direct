export interface OrderProcessing {
  status: string;
  version: number;
  history: {
    operationId: string;
    version: number;
    fromStatus: string;
    toStatus: string;
    reason: string;
    actor: string;
    createdAt: string;
  }[];
}
