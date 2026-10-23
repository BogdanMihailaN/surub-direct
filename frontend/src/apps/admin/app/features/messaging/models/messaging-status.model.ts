import { FailedMessage } from './failed-message.model';
export interface MessagingStatus {
  inboxTotal: number;
  lastReceivedAt: string | null;
  awaitingReview: number;
  pendingReplays: number;
  items: FailedMessage[];
  total: number;
  page: number;
  pageSize: number;
}
