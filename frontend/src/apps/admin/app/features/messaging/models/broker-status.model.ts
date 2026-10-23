export interface BrokerStatus {
  configured: boolean;
  available: boolean;
  ready: number | null;
  deadReady: number | null;
  checkedAt: string;
}
