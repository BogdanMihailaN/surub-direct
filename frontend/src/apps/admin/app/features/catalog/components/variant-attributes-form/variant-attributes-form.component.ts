import { Component, computed, effect, input, output } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { TechnicalAttribute, Variant } from '@shared/models';
import { SetVariantAttributeRequest } from '../../models/set-variant-attribute.request';
@Component({
  selector: 'sd-variant-attributes-form',
  imports: [FormsModule],
  templateUrl: './variant-attributes-form.component.html',
  styleUrl: './variant-attributes-form.component.scss',
})
export class VariantAttributesForm {
  readonly variant = input.required<Variant>();
  readonly definitions = input.required<TechnicalAttribute[]>();
  readonly busy = input(false);
  readonly submitted = output<SetVariantAttributeRequest[]>();
  readonly cancelled = output<void>();
  readonly custom = computed(() => this.definitions().filter((a) => a.source === 'Custom'));
  values: Record<string, { text: string; number: number | null }> = {};
  constructor() {
    effect(() => {
      const existing = this.variant().attributes ?? [];
      this.values = Object.fromEntries(
        this.custom().map((a) => {
          const v = existing.find((v) => v.attributeId === a.id);
          return [a.id, { text: v?.textValue ?? '', number: v?.numberValue ?? null }];
        }),
      );
    });
  }
  valid() {
    return Object.values(this.values).every(
      (v) =>
        v.number === null ||
        (Number.isFinite(v.number) &&
          Math.abs(v.number) <= 999999999999.999999 &&
          Math.abs(Math.round(v.number * 1e6) - v.number * 1e6) < 0.0001),
    );
  }
  submit(form: NgForm) {
    if (this.busy() || form.invalid || !this.valid()) return;
    const values = this.custom().flatMap<SetVariantAttributeRequest>((a) => {
      const v = this.values[a.id];
      if (a.kind === 'Text')
        return v.text.trim()
          ? [{ attributeId: a.id, textValue: v.text.trim(), numberValue: null }]
          : [];
      return v.number !== null
        ? [{ attributeId: a.id, textValue: null, numberValue: v.number }]
        : [];
    });
    this.submitted.emit(values);
  }
}
