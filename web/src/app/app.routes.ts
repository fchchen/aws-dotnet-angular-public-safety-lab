import { Routes } from '@angular/router';
import { IncidentsDashboardComponent } from './features/incidents/incidents-dashboard.component';
import { IncidentDetailComponent } from './features/incidents/incident-detail.component';

export const routes: Routes = [
  {
    path: '',
    component: IncidentsDashboardComponent
  },
  {
    path: 'incidents/:incidentId',
    component: IncidentDetailComponent
  }
];
