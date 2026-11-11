export interface Vehicle {
  id: string;
  plate: string;
  workshop: string;
  odometer: number;
  version: number;
}

export interface Routine {
  id: string;
  vehicleId: string;
  name: string;
  intervalKm: number | null;
  intervalDays: number | null;
  lastServiceKm: number;
  lastServiceDate: string;
  cycleId: string;
  status: string;
  noticeRaised: boolean;
  cycleCancelled: boolean;
  version: number;
}

export interface Order {
  id: string;
  vehicleId: string;
  routineId: string;
  workshop: string;
  state: string;
  quoteAmount: number | null;
  actualKm: number | null;
  actualDate: string | null;
  version: number;
  createdAt: string;
}

export interface DomainEvent {
  id: string;
  type: string;
  aggregateId: string;
  cycleId: string;
  message: string;
  occurredAt: string;
  publishedAt: string | null;
}

export interface Dashboard {
  vehicles: Vehicle[];
  routines: Routine[];
  orders: Order[];
  events: DomainEvent[];
  today: string;
}

export const STATE_LABELS: Record<string, string> = {
  Current: 'Al día',
  DueSoon: 'Por vencer',
  Overdue: 'Vencida',
  Created: 'Creada',
  Quoted: 'Por aprobar',
  Approved: 'Aprobada',
  InProgress: 'En ejecución',
  Completed: 'Completada',
  Cancelled: 'Cancelada'
};
