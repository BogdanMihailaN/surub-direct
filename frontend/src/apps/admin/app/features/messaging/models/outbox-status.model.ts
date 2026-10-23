export interface OutboxStatus {
  pending: number;
  oldestPendingAt: string | null;
  lastPublishedAt: string | null;
}
