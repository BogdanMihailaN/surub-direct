import { Component, effect, input, output } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { TechnicalAttribute } from '@shared/models';
import { AttributeWriteRequest } from '../../models/attribute-write.request';
@Component({
  selector: 'sd-attribute-form',
  imports: [FormsModule],
  templateUrl: './attribute-form.component.html',
  styleUrl: './attribute-form.component.scss',
})
export class AttributeForm {
  readonly attribute = input<TechnicalAttribute | null>(null);
  readonly busy = input(false);
  readonly submitted = output<AttributeWriteRequest>();
  details: AttributeWriteRequest = { code: '', name: '', kind: 'Text', unit: null, sortOrder: 0 };
  constructor() {
    effect(() => {
      const a = this.attribute();
      this.details = a
        ? { code: a.code, name: a.name, kind: a.kind, unit: a.unit, sortOrder: a.sortOrder }
        : { code: '', name: '', kind: 'Text', unit: null, sortOrder: 0 };
    });
  }
  submit(form: NgForm) {
    if (form.invalid || this.busy() || !Number.isInteger(this.details.sortOrder)) return;
    this.submitted.emit({
      ...this.details,
      unit: this.details.kind === 'Text' ? null : this.details.unit,
    });
  }
}
