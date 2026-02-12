import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';
import {
  CreateIncidentRequest,
  IncidentDetailDto,
  IncidentSummaryDto,
  QueueIncidentProcessingRequest,
  UploadEvidenceUrlRequest,
  UploadEvidenceUrlResponse
} from './incident.models';

@Injectable({ providedIn: 'root' })
export class IncidentApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  listIncidents(status?: string): Observable<IncidentSummaryDto[]> {
    const params = status ? { status } : {};
    return this.http.get<IncidentSummaryDto[]>(`${this.baseUrl}/incidents`, { params });
  }

  createIncident(request: CreateIncidentRequest): Observable<IncidentDetailDto> {
    return this.http.post<IncidentDetailDto>(`${this.baseUrl}/incidents`, request);
  }

  getIncident(incidentId: string): Observable<IncidentDetailDto> {
    return this.http.get<IncidentDetailDto>(`${this.baseUrl}/incidents/${incidentId}`);
  }

  createUploadUrl(
    incidentId: string,
    request: UploadEvidenceUrlRequest
  ): Observable<UploadEvidenceUrlResponse> {
    return this.http.post<UploadEvidenceUrlResponse>(
      `${this.baseUrl}/incidents/${incidentId}/evidence/upload-url`,
      request
    );
  }

  queueProcessing(
    incidentId: string,
    request: QueueIncidentProcessingRequest
  ): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/incidents/${incidentId}/process`, request);
  }
}
