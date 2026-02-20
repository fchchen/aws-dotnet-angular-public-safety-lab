import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { API_BASE_URL } from './api-base-url.token';
import { IncidentApiService } from './incident-api.service';

describe('IncidentApiService', () => {
  let service: IncidentApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: '/api/v1' }
      ]
    });

    service = TestBed.inject(IncidentApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should call GET /incidents when listing incidents', () => {
    service.listIncidents().subscribe();

    const request = httpMock.expectOne('/api/v1/incidents');
    expect(request.request.method).toBe('GET');
    expect(request.request.headers.get('X-Api-Key')).toBe('demo-api-key');
    request.flush([]);
  });
});
