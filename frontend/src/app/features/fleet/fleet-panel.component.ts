import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Dashboard, Vehicle } from '../../models/dashboard.models';
import { StatusBadgeComponent } from '../../shared/status-badge.component';

@Component({
  selector: 'app-fleet-panel',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StatusBadgeComponent],
  templateUrl: './fleet-panel.component.html'
})
export class FleetPanelComponent {
  private fb = inject(FormBuilder);

  @Input({ required: true }) data!: Dashboard;
  @Input({ required: true }) selectedId!: string;
  @Input() busy = false;
  @Output() selectVehicle = new EventEmitter<string>();
  @Output() createVehicle = new EventEmitter<{ plate: string; workshop: string; odometer: number }>();
  @Output() createRoutine = new EventEmitter<{
    name: string;
    intervalKm: number | null;
    intervalDays: number | null;
    lastServiceKm: number;
    lastServiceDate: string;
  }>();
  @Output() sendReading = new EventEmitter<number>();
  @Output() evaluate = new EventEmitter<void>();

  showVehicle = false;
  showRoutine = false;

  vehicleForm = this.fb.nonNullable.group({
    plate: ['', [Validators.required, Validators.maxLength(20)]],
    workshop: ['', [Validators.required, Validators.maxLength(120)]],
    odometer: [0, [Validators.required, Validators.min(0)]]
  });

  routineForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    intervalKm: this.fb.control<number | null>(10000),
    intervalDays: this.fb.control<number | null>(180),
    lastServiceKm: [0, [Validators.required, Validators.min(0)]],
    lastServiceDate: ['', Validators.required]
  });

  readingForm = this.fb.nonNullable.group({
    kilometres: [0, [Validators.required, Validators.min(0)]]
  });

  get vehicleRoutines() {
    return this.data.routines.filter(r => r.vehicleId === this.selectedId);
  }

  get currentVehicle(): Vehicle | null {
    return this.data.vehicles.find(v => v.id === this.selectedId) ?? null;
  }

  onSelect(id: string) {
    this.selectVehicle.emit(id);
    const vehicle = this.data.vehicles.find(v => v.id === id);
    this.readingForm.patchValue({ kilometres: vehicle?.odometer ?? 0 });
    this.routineForm.patchValue({
      lastServiceKm: vehicle?.odometer ?? 0,
      lastServiceDate: this.data.today
    });
  }

  openVehicle() {
    this.showVehicle = true;
    this.vehicleForm.reset({ plate: '', workshop: '', odometer: 0 });
  }

  openRoutine() {
    this.showRoutine = true;
    const vehicle = this.currentVehicle;
    this.routineForm.reset({
      name: '',
      intervalKm: 10000,
      intervalDays: 180,
      lastServiceKm: vehicle?.odometer ?? 0,
      lastServiceDate: this.data.today
    });
  }

  submitVehicle() {
    if (this.vehicleForm.invalid || this.busy) return;
    this.createVehicle.emit(this.vehicleForm.getRawValue());
    this.showVehicle = false;
  }

  submitRoutine() {
    if (this.routineForm.invalid || this.busy) return;
    const value = this.routineForm.getRawValue();
    this.createRoutine.emit({
      name: value.name || '',
      intervalKm: value.intervalKm,
      intervalDays: value.intervalDays,
      lastServiceKm: Number(value.lastServiceKm),
      lastServiceDate: value.lastServiceDate || this.data.today
    });
    this.showRoutine = false;
  }

  submitReading() {
    if (this.readingForm.invalid || this.busy) return;
    this.sendReading.emit(this.readingForm.controls.kilometres.value);
  }

  syncReading(vehicle: Vehicle | null | undefined) {
    if (vehicle) this.readingForm.patchValue({ kilometres: vehicle.odometer });
  }
}
