import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
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

  readonly hasIncident = computed(() => this.incident() !== null);

  readonly uploadForm = this.formBuilder.nonNullable.group({
    fileName: ['', Validators.required],
    contentType: ['image/jpeg', Validators.required]
  });

  readonly processForm = this.formBuilder.nonNullable.group({
    reason: ['Auto-dispatch from UI']
  });

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
          this.reload(current.incidentId);
        },
        error: () => {
          this.error.set('Unable to create upload URL.');
        }
      });
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
