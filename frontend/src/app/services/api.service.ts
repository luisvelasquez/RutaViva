import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { Dashboard } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  // Conserva la clave tras error de red/5xx. Ante resultado incierto no genera otra clave.
  private pending = new Map<string, string>(JSON.parse(sessionStorage.getItem('rutaviva-pending') || '[]'));

  dashboard() {
    return firstValueFrom(this.http.get<Dashboard>('/api/dashboard'));
  }

  async post(path: string, body: unknown) {
    const signature = path + ':' + JSON.stringify(body);
    const key = this.pending.get(signature) || crypto.randomUUID();
    this.pending.set(signature, key);
    this.persist();
    try {
      const result = await firstValueFrom(
        this.http.post('/api' + path, body, { headers: new HttpHeaders({ 'Idempotency-Key': key }) })
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
