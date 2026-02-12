export type IncidentStatus = 'New' | 'Queued' | 'Processed' | 'Failed';

export interface CreateIncidentRequest {
  title: string;
  description: string;
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  location: string;
  reportedAt: string;
}

export interface IncidentSummaryDto {
  incidentId: string;
  title: string;
  priority: string;
  location: string;
  status: IncidentStatus;
  createdAt: string;
  reportedAt: string;
}

export interface EvidenceDto {
  fileName: string;
  objectKey: string;
  uploadedAt: string;
}

export interface IncidentDetailDto {
  incidentId: string;
  tenantId: string;
  title: string;
  description: string;
  priority: string;
  location: string;
  status: IncidentStatus;
  createdAt: string;
  reportedAt: string;
  queuedAt: string | null;
  processedAt: string | null;
  failureReason: string | null;
  evidence: EvidenceDto[];
}

export interface UploadEvidenceUrlRequest {
  fileName: string;
  contentType: string;
}

export interface UploadEvidenceUrlResponse {
  uploadUrl: string;
  objectKey: string;
  expiresAt: string;
}

export interface QueueIncidentProcessingRequest {
  reason: string | null;
}
