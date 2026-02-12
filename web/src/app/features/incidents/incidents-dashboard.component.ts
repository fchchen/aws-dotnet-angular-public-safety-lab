import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { IncidentApiService } from '../../core/incident-api.service';
import { IncidentSummaryDto } from '../../core/incident.models';

@Component({
  selector: 'app-incidents-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './incidents-dashboard.component.html',
  styleUrl: './incidents-dashboard.component.scss'
})
export class IncidentsDashboardComponent implements OnInit {
  private readonly incidentApiService = inject(IncidentApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly incidents = signal<IncidentSummaryDto[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(3)]],
    description: ['', [Validators.required, Validators.minLength(10)]],
    priority: this.formBuilder.nonNullable.control<'Low' | 'Medium' | 'High' | 'Critical'>('Medium'),
    location: ['', Validators.required]
  });

  ngOnInit(): void {
    this.refreshIncidents();
  }

  refreshIncidents(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.incidentApiService
      .listIncidents()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (incidents) => {
          this.incidents.set(incidents);
          this.isLoading.set(false);
        },
        error: () => {
          this.error.set('Unable to load incidents right now.');
          this.isLoading.set(false);
        }
      });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.error.set(null);

    this.incidentApiService
      .createIncident({
        title: this.form.controls.title.value,
        description: this.form.controls.description.value,
        priority: this.form.controls.priority.value,
        location: this.form.controls.location.value,
        reportedAt: new Date().toISOString()
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.form.reset({
            title: '',
            description: '',
            priority: 'Medium',
            location: ''
          });
          void this.router.navigate(['/incidents', created.incidentId]);
        },
        error: () => {
          this.error.set('Unable to create incident. Check input and retry.');
        }
      });
  }
}
