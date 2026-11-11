import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Dashboard, Order, STATE_LABELS } from '../../models/dashboard.models';
import { StatusBadgeComponent } from '../../shared/status-badge.component';

@Component({
  selector: 'app-orders-panel',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StatusBadgeComponent],
  templateUrl: './orders-panel.component.html'
})
export class OrdersPanelComponent {
  private fb = inject(FormBuilder);

  @Input({ required: true }) data!: Dashboard;
  @Input() busy = false;
  @Output() orderAction = new EventEmitter<{ order: Order; action: string; body: Record<string, unknown> }>();

  labels = STATE_LABELS;
  activeOrder: Order | null = null;
  action = '';

  actionForm = this.fb.group({
    amount: [1500000, [Validators.required, Validators.min(0.01)]],
    kilometres: [0, [Validators.required, Validators.min(0)]],
    date: ['', Validators.required]
  });

  plate(id: string) {
    return this.data.vehicles.find(v => v.id === id)?.plate || id;
  }

  routineName(id: string) {
    return this.data.routines.find(r => r.id === id)?.name || id;
  }

  actionLabel() {
    return ({
      quote: 'Enviar cotización',
      approve: 'Aprobar cotización',
      reject: 'Rechazar cotización',
      cancel: 'Cancelar orden',
      start: 'Iniciar servicio',
      complete: 'Completar servicio'
    } as Record<string, string>)[this.action];
  }

  openAction(order: Order, action: string) {
    this.activeOrder = order;
    this.action = action;
    this.actionForm.reset({
      amount: 1500000,
      kilometres: this.data.vehicles.find(v => v.id === order.vehicleId)?.odometer || 0,
      date: this.data.today
    });
  }

  submitAction() {
    const order = this.activeOrder;
    if (!order || this.busy) return;
    if ((this.action === 'quote' || this.action === 'complete') && this.actionForm.invalid) return;

    const raw = this.actionForm.getRawValue();
    const body: Record<string, unknown> = { expectedVersion: order.version };
    if (this.action === 'quote') body['amount'] = raw.amount;
    if (this.action === 'complete') {
      body['kilometres'] = raw.kilometres;
      body['date'] = raw.date;
    }
    // El padre envía PATCH /api/orders/{id} con operation = action.
    this.orderAction.emit({ order, action: this.action, body });
    this.activeOrder = null;
  }
}
