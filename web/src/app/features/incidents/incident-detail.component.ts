import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom, switchMap } from 'rxjs';
import { IncidentApiService } from '../../core/incident-api.service';
import { IncidentDetailDto } from '../../core/incident.models';

@Component({
  selector: 'app-incident-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './incident-detail.component.html',
  styleUrl: './incident-detail.component.scss'
})
export class IncidentDetailComponent implements OnInit {
  private readonly incidentApiService = inject(IncidentApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly incident = signal<IncidentDetailDto | null>(null);
  readonly error = signal<string | null>(null);
  readonly uploadResult = signal<string | null>(null);
  readonly uploadStatus = signal<string | null>(null);
  readonly uploadInProgress = signal(false);
  readonly selectedFileName = signal<string | null>(null);
  readonly selectedContentType = signal<string | null>(null);

  readonly hasIncident = computed(() => this.incident() !== null);

  readonly uploadForm = this.formBuilder.nonNullable.group({
    fileName: ['', Validators.required],
    contentType: ['image/jpeg', Validators.required]
  });

  readonly processForm = this.formBuilder.nonNullable.group({
    reason: ['Auto-dispatch from UI']
  });

  private selectedFile: File | null = null;

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap((params) => {
          const incidentId = params.get('incidentId');
          if (!incidentId) {
            throw new Error('incidentId route param is required');
          }
          return this.incidentApiService.getIncident(incidentId);
        })
      )
      .subscribe({
        next: (incident) => {
          this.incident.set(incident);
          this.error.set(null);
        },
        error: () => {
          this.error.set('Unable to load incident details.');
        }
      });
  }

  requestUploadUrl(): void {
    const current = this.incident();
    if (!current || this.uploadForm.invalid) {
      return;
    }

    this.uploadResult.set(null);

    this.incidentApiService
      .createUploadUrl(current.incidentId, {
        fileName: this.uploadForm.controls.fileName.value,
        contentType: this.uploadForm.controls.contentType.value
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.uploadResult.set(result.uploadUrl);
          this.uploadStatus.set('Upload URL generated.');
          this.reload(current.incidentId);
        },
        error: () => {
          this.error.set('Unable to create upload URL.');
        }
      });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    this.selectedFile = file;
    this.uploadStatus.set(null);

    if (!file) {
      this.selectedFileName.set(null);
      this.selectedContentType.set(null);
      return;
    }

    this.selectedFileName.set(file.name);
    this.selectedContentType.set(file.type || 'image/jpeg');
    this.uploadForm.patchValue({
      fileName: file.name,
      contentType: file.type || 'image/jpeg'
    });
  }

  async uploadSelectedFile(): Promise<void> {
    const current = this.incident();
    if (!current || !this.selectedFile) {
      this.error.set('Select an image file first.');
      return;
    }

    this.error.set(null);
    this.uploadStatus.set(null);
    this.uploadInProgress.set(true);

    try {
      const upload = await firstValueFrom(
        this.incidentApiService.createUploadUrl(current.incidentId, {
          fileName: this.selectedFile.name,
          contentType: this.selectedFile.type || 'image/jpeg'
        })
      );

      this.uploadResult.set(upload.uploadUrl);

      // Local in-memory mode returns a placeholder URL and skips real object upload.
      if (upload.uploadUrl.includes('local-upload.invalid')) {
        this.uploadStatus.set('Local mode: metadata saved. Real object upload requires AWS mode.');
      } else {
        const response = await fetch(upload.uploadUrl, {
          method: 'PUT',
          headers: {
            'Content-Type': this.selectedFile.type || 'application/octet-stream'
          },
          body: this.selectedFile
        });

        if (!response.ok) {
          throw new Error(`Upload failed with status ${response.status}`);
        }

        this.uploadStatus.set('Image uploaded successfully.');
      }

      this.reload(current.incidentId);
    } catch {
      this.error.set('Unable to upload image. Verify AWS mode/S3 CORS configuration and retry.');
    } finally {
      this.uploadInProgress.set(false);
    }
  }

  queueProcessing(): void {
    const current = this.incident();
    if (!current) {
      return;
    }

    this.incidentApiService
      .queueProcessing(current.incidentId, {
        reason: this.processForm.controls.reason.value || null
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.reload(current.incidentId);
        },
        error: () => {
          this.error.set('Unable to queue processing.');
        }
      });
  }

  private reload(incidentId: string): void {
    this.incidentApiService
      .getIncident(incidentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (incident) => this.incident.set(incident),
        error: () => this.error.set('Unable to refresh incident details.')
      });
  }
}
