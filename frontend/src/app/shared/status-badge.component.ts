import { Component, Input } from '@angular/core';
import { STATE_LABELS } from '../models/dashboard.models';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `<span class="badge" [attr.data-state]="state">{{ label }}</span>`
})
export class StatusBadgeComponent {
  @Input({ required: true }) state!: string;
  @Input() cancelled = false;

  get label() {
    if (this.cancelled) return 'Ciclo cancelado';
    return STATE_LABELS[this.state] || this.state;
  }
}
