import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { provideRouter } from '@angular/router';
import { IncidentsDashboardComponent } from './incidents-dashboard.component';
import { IncidentApiService } from '../../core/incident-api.service';

describe('IncidentsDashboardComponent', () => {
  it('should load incidents on init', () => {
    const apiSpy = jasmine.createSpyObj<IncidentApiService>('IncidentApiService', [
      'listIncidents',
      'createIncident'
    ]);

    apiSpy.listIncidents.and.returnValue(of([]));
    apiSpy.createIncident.and.returnValue(
      of({
        incidentId: 'abc',
        tenantId: 'demo',
        title: 'title',
        description: 'description',
        priority: 'High',
        location: 'location',
        status: 'New',
        createdAt: new Date().toISOString(),
        reportedAt: new Date().toISOString(),
        queuedAt: null,
        processedAt: null,
        failureReason: null,
        evidence: []
      })
    );

    TestBed.configureTestingModule({
      imports: [IncidentsDashboardComponent],
      providers: [provideRouter([]), { provide: IncidentApiService, useValue: apiSpy }]
    });

    const fixture = TestBed.createComponent(IncidentsDashboardComponent);
    fixture.detectChanges();

    expect(apiSpy.listIncidents).toHaveBeenCalledTimes(1);
  });
});
