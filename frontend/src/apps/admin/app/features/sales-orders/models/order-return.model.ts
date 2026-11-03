export interface OrderReturn {
  status: string;
  version: number;
  restoredAt: string | null;
  history: { operationId: string; version: number; fromStatus: string; toStatus: string; reason: string; actor: string; createdAt: string }[];
}
