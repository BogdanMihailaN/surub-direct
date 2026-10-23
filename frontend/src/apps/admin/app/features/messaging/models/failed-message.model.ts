export interface FailedMessage {
  id: string;
  messageId: string | null;
  messageType: string | null;
  previousReplayId: string | null;
  originalSize: number;
  attempts: number;
  canReplay: boolean;
  capturedAt: string;
  replayRequestId: string | null;
  replayReason: string | null;
  requestedBy: string | null;
  requestedAt: string | null;
  publishedAt: string | null;
  payloadRemovedAt: string | null;
}
