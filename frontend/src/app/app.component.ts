import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiService } from './services/api.service';
import { Dashboard, Order } from './models/dashboard.models';
import { FleetPanelComponent } from './features/fleet/fleet-panel.component';
import { OrdersPanelComponent } from './features/orders/orders-panel.component';
import { EventsPanelComponent } from './features/events/events-panel.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FleetPanelComponent, OrdersPanelComponent, EventsPanelComponent],
  templateUrl: './app.component.html'
})
export class AppComponent {
  private api = inject(ApiService);
  private fleet = viewChild(FleetPanelComponent);

  data = signal<Dashboard>({ vehicles: [], routines: [], orders: [], events: [], today: '' });
  tab = signal('fleet');
  busy = signal(false);
  error = signal('');
  message = signal('');
  loaded = signal(false);
  selected = signal('');

  openOrders = computed(() => this.data().orders.filter(o => !['Completed', 'Cancelled'].includes(o.state)).length);
  dueSoon = computed(() => this.data().routines.filter(r => r.status === 'DueSoon' && !r.cycleCancelled).length);
  overdue = computed(() => this.data().routines.filter(r => r.status === 'Overdue' && !r.cycleCancelled).length);

  constructor() {
    void this.refresh();
  }

  async refresh() {
    this.error.set('');
    try {
      const d = await this.api.dashboard();
      this.data.set(d);
      this.loaded.set(true);
      if (!this.selected() && d.vehicles.length) this.selectVehicle(d.vehicles[0].id);
    } catch (e) {
      this.report(e);
    }
  }

  selectVehicle(id: string) {
    this.selected.set(id);
    this.fleet()?.syncReading(this.data().vehicles.find(v => v.id === id));
  }

  async execute(path: string, body: unknown, success: string) {
    if (this.busy()) return null;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      const result = await this.api.post(path, body);
      this.message.set(success);
      await this.refresh();
      return result;
    } catch (e) {
      this.report(e);
      if (e instanceof HttpErrorResponse && e.status === 409) await this.reloadAfterConflict();
      return null;
    } finally {
      this.busy.set(false);
    }
  }

  private async reloadAfterConflict() {
    try {
      this.data.set(await this.api.dashboard());
    } catch {
      /* Conserva el error original. */
    }
  }

  async addVehicle(input: { plate: string; workshop: string; odometer: number }) {
    const result = await this.execute('/vehicles', input, 'Vehículo registrado.');
    if (result && typeof result === 'object' && 'id' in result) this.selectVehicle(String(result.id));
  }

  addRoutine(input: {
    name: string;
    intervalKm: number | null;
    intervalDays: number | null;
    lastServiceKm: number;
    lastServiceDate: string;
  }) {
    return this.execute(`/vehicles/${this.selected()}/routines`, input, 'Rutina registrada y evaluada.');
  }

  async sendReading(kilometres: number) {
    const result = await this.execute(`/vehicles/${this.selected()}/readings`, { kilometres }, 'Lectura procesada.');
    if (result && typeof result === 'object' && 'accepted' in result) {
      this.message.set(
        (result as { accepted: boolean }).accepted
          ? 'Lectura aceptada. Rutinas evaluadas.'
          : 'Lectura ignorada: es menor o igual al odómetro actual.'
      );
    }
  }

  evaluate() {
    return this.execute('/maintenance/evaluate', {}, 'Evaluación diaria completada.');
  }

  submitOrderAction(event: { order: Order; action: string; body: Record<string, unknown> }) {
    return this.execute(`/orders/${event.order.id}/${event.action}`, event.body, 'Orden actualizada.');
  }

  private report(e: unknown) {
    this.error.set(
      e instanceof HttpErrorResponse
        ? e.error?.title || 'No se pudo conectar con la API. Puede reintentar la operación.'
        : 'Ocurrió un error inesperado.'
    );
  }
}
