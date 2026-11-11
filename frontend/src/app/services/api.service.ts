import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpHeaders, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { Dashboard, Order, Routine, Vehicle } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  // Conserva la clave tras error de red/5xx. Ante resultado incierto no genera otra clave.
  private pending = new Map<string, string>(JSON.parse(sessionStorage.getItem('rutaviva-pending') || '[]'));

  dashboard() {
    return firstValueFrom(this.http.get<Dashboard>('/api/dashboard'));
  }

  listVehicles() {
    return firstValueFrom(this.http.get<Vehicle[]>('/api/vehicles'));
  }

  getVehicle(vehicleId: string) {
    return firstValueFrom(this.http.get<Vehicle>(`/api/vehicles/${vehicleId}`));
  }

  listVehicleRoutines(vehicleId: string) {
    return firstValueFrom(this.http.get<Routine[]>(`/api/vehicles/${vehicleId}/routines`));
  }

  listOrders(filters?: { vehicleId?: string; status?: string }) {
    let params = new HttpParams();
    if (filters?.vehicleId) params = params.set('vehicleId', filters.vehicleId);
    if (filters?.status) params = params.set('status', filters.status);
    return firstValueFrom(this.http.get<Order[]>('/api/orders', { params }));
  }

  getOrder(orderId: string) {
    return firstValueFrom(this.http.get<Order>(`/api/orders/${orderId}`));
  }

  post(path: string, body: unknown) {
    return this.write('POST', path, body);
  }

  patch(path: string, body: unknown) {
    return this.write('PATCH', path, body);
  }

  private async write(method: 'POST' | 'PATCH', path: string, body: unknown) {
    const signature = method + ':' + path + ':' + JSON.stringify(body);
    const key = this.pending.get(signature) || crypto.randomUUID();
    this.pending.set(signature, key);
    this.persist();
    try {
      const result = await firstValueFrom(
        this.http.request(method, '/api' + path, {
          body,
          headers: new HttpHeaders({ 'Idempotency-Key': key })
        })
      );
      this.pending.delete(signature);
      this.persist();
      return result;
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status > 0 && error.status < 500) {
        this.pending.delete(signature);
        this.persist();
      }
      throw error;
    }
  }

  private persist() {
    sessionStorage.setItem('rutaviva-pending', JSON.stringify([...this.pending]));
  }
}
