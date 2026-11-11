import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Dashboard } from '../../models/dashboard.models';

@Component({
  selector: 'app-events-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './events-panel.component.html'
})
export class EventsPanelComponent {
  @Input({ required: true }) data!: Dashboard;
}
