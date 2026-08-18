import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { TopSellingItemDto } from '../../../shared/models/reporting.models';
import { ReportingService } from '../reporting.service';

// N-C — split out of the former ReportsDashboardComponent verbatim.
@Component({
  selector: 'app-top-selling',
  imports: [
    DecimalPipe,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    PageHeaderComponent,
    TranslatePipe,
  ],
  templateUrl: './top-selling.component.html',
  styleUrl: './top-selling.component.scss',
})
export class TopSellingComponent implements OnInit {
  readonly loading = signal(false);
  readonly topSelling = signal<TopSellingItemDto[]>([]);
  readonly takeControl = new FormControl(10, { nonNullable: true });

  readonly topSellingMax = computed(() => Math.max(...this.topSelling().map((r) => r.totalRevenue), 0));

  constructor(private readonly reportingService: ReportingService) {}

  ngOnInit(): void {
    this.load();
    this.takeControl.valueChanges.subscribe(() => this.load());
  }

  private load(): void {
    this.loading.set(true);
    this.reportingService.getTopSellingItems(this.takeControl.value).subscribe({
      next: (top) => {
        this.topSelling.set(top);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
