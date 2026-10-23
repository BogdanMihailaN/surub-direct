import { computed, inject, Injectable, signal } from '@angular/core';
import { errorMessage } from '@shared/http/error-message';
import { evaluateMessagingAlerts } from './evaluate-messaging-alerts';
import { MessagingApi } from '../data-access/messaging-api.service';
import { MessagingStatus } from '../models/messaging-status.model';
import { BrokerStatus } from '../models/broker-status.model';
import { OutboxStatus } from '../models/outbox-status.model';
@Injectable()
export class MessagingStore {
  private readonly api = inject(MessagingApi);
  readonly observedAt = signal<number | null>(null);
  readonly alerts = computed(() =>
    evaluateMessagingAlerts(this.data(), this.broker(), this.outbox(), this.observedAt() ?? 0),
  );
  readonly data = signal<MessagingStatus | null>(null);
  readonly broker = signal<BrokerStatus | null>(null);
  readonly outbox = signal<OutboxStatus | null>(null);
  readonly errors = signal<string[]>([]);
  readonly actionError = signal('');
  readonly notice = signal('');
  readonly busy = signal(false);
  readonly saving = signal(false);
  readonly selectedId = signal<string | null>(null);
  readonly selected = computed(
    () => this.data()?.items.find((x) => x.id === this.selectedId()) ?? null,
  );
  private requestId = '';
  select(id: string) {
    if (this.saving()) return;
    this.selectedId.set(id);
    this.requestId = crypto.randomUUID();
    this.actionError.set('');
    this.notice.set('');
  }
  async load(page = this.data()?.page ?? 1) {
    if (this.busy()) return;
    this.busy.set(true);
    this.errors.set([]);
    const [data, broker, outbox] = await Promise.allSettled([
      this.api.read(page),
      this.api.broker(),
      this.api.outbox(),
    ]);
    const errors: string[] = [];
    if (data.status === 'fulfilled') this.data.set(data.value);
    else {
      this.data.set(null);
      errors.push('Ordering: ' + errorMessage(data.reason));
    }
    if (broker.status === 'fulfilled') this.broker.set(broker.value);
    else {
      this.broker.set(null);
      errors.push('Broker: ' + errorMessage(broker.reason));
    }
    if (outbox.status === 'fulfilled') this.outbox.set(outbox.value);
    else {
      this.outbox.set(null);
      errors.push('Inventory: ' + errorMessage(outbox.reason));
    }
    this.observedAt.set(Date.now());
    this.errors.set(errors);
    this.busy.set(false);
  }
  async replay(reason: string) {
    const selected = this.selected();
    if (!selected || this.saving() || !this.requestId) return;
    this.saving.set(true);
    this.actionError.set('');
    this.notice.set('');
    try {
      await this.api.replay(selected.id, this.requestId, reason);
      this.notice.set('Cererea a fost salvată. Publicarea și procesarea se verifică separat.');
    } catch (e) {
      this.actionError.set(errorMessage(e) + ' Actualizează lista înainte de a reîncerca.');
    } finally {
      await this.load();
      this.saving.set(false);
    }
  }
}
