import { BrokerStatus } from '../models/broker-status.model';
import { MessagingStatus } from '../models/messaging-status.model';
import { OutboxStatus } from '../models/outbox-status.model';
import { MessagingAlert } from '../models/messaging-alert.model';

export const messagingAlertThresholds = { outboxAgeMs: 5 * 60_000, readyMessages: 100 };

export function evaluateMessagingAlerts(
  data: MessagingStatus | null,
  broker: BrokerStatus | null,
  outbox: OutboxStatus | null,
  observedAt: number,
): MessagingAlert[] {
  const alerts: MessagingAlert[] = [];
  const add = (code: string, severity: MessagingAlert['severity'], title: string, detail: string) =>
    alerts.push({ code, severity, title, detail });
  if (!broker)
    add(
      'broker-unknown',
      'critical',
      'Starea brokerului nu poate fi verificată',
      'Verifică accesul la Ordering și reîncarcă datele. Lipsa răspunsului nu confirmă oprirea brokerului.',
    );
  else if (!broker.configured)
    add(
      'broker-disabled',
      'warning',
      'Mesageria nu este configurată',
      'Verifică setările mediului. Reluarea mesajelor nu este disponibilă.',
    );
  else if (!broker.available)
    add(
      'broker-offline',
      'critical',
      'Broker indisponibil',
      'Verifică serviciul RabbitMQ și conexiunea din Ordering. Cererile persistente rămân în așteptare.',
    );
  else {
    if ((broker.deadReady ?? 0) > 0)
      add(
        'dead-queue',
        'warning',
        'Mesaje în coada de erori',
        `Mesaje care așteaptă arhivarea: ${broker.deadReady}. Verifică worker-ul Ordering dacă numărul persistă.`,
      );
    if ((broker.ready ?? 0) >= messagingAlertThresholds.readyMessages)
      add(
        'ready-backlog',
        'warning',
        'Coada de procesare este aglomerată',
        `${broker.ready} mesaje gata de preluare (prag: 100). Verifică procesarea în Ordering; mesajele în curs nu sunt incluse.`,
      );
  }
  if (!data)
    add(
      'ordering-unknown',
      'critical',
      'Registrul Ordering nu poate fi verificat',
      'Verifică disponibilitatea serviciului și accesul administrativ înainte de a concluziona că nu există erori.',
    );
  else {
    if (data.awaitingReview > 0)
      add(
        'review',
        'warning',
        'Mesaje care necesită investigare',
        `${data.awaitingReview} mesaje fără cerere de reluare, inclusiv contracte invalide. Verifică registrul și cauza înainte de reluare.`,
      );
    if (data.pendingReplays > 0)
      add(
        'replays',
        'warning',
        'Reluări în așteptarea publicării',
        `${data.pendingReplays} cereri salvate. Verifică brokerul și worker-ul; nu crea cereri duplicate.`,
      );
  }
  if (!outbox)
    add(
      'inventory-unknown',
      'critical',
      'Outbox Inventory nu poate fi verificat',
      'Verifică disponibilitatea Inventory. Numărul mesajelor restante este necunoscut.',
    );
  else if (outbox.pending > 0) {
    const oldest = outbox.oldestPendingAt ? Date.parse(outbox.oldestPendingAt) : NaN;
    if (!Number.isFinite(oldest) || oldest > observedAt)
      add(
        'outbox-age-unknown',
        'warning',
        'Vechimea mesajelor Outbox este necunoscută',
        'Există mesaje restante. Verifică timestamp-urile și sincronizarea ceasurilor serviciilor.',
      );
    else if (observedAt - oldest >= messagingAlertThresholds.outboxAgeMs)
      add(
        'outbox-delayed',
        'critical',
        'Publicarea din Inventory întârzie',
        `${outbox.pending} mesaje restante; cel mai vechi așteaptă de cel puțin 5 minute. Verifică publisher-ul și conexiunea la RabbitMQ.`,
      );
  }
  return alerts.sort(
    (a, b) => Number(b.severity === 'critical') - Number(a.severity === 'critical'),
  );
}
