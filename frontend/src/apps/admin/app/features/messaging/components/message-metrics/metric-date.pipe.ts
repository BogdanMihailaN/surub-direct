import { inject, LOCALE_ID, Pipe, PipeTransform } from '@angular/core';
import { DatePipe } from '@angular/common';

@Pipe({ name: 'metricDate' })
export class MetricDatePipe implements PipeTransform {
  private readonly date = new DatePipe(inject(LOCALE_ID));
  transform(value: string | null): string {
    if (!value) return '—';
    const timestamp = Date.parse(value);
    return Number.isFinite(timestamp)
      ? (this.date.transform(timestamp, 'dd.MM.yyyy HH:mm:ss') ?? '—')
      : 'Necunoscută';
  }
}
