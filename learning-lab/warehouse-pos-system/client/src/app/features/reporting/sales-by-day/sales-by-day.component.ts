import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SalesByDayDto } from '../../../shared/models/reporting.models';
import { ReportingService } from '../reporting.service';

// One bar's full render geometry, precomputed once per data load rather than
// in the template — the template only reads fields, it never does chart math.
interface SalesBar {
  dateLabel: string;
  fullDate: string;
  value: number;
  path: string;
  labelX: number;
  labelY: number;
  axisLabelX: number;
  showValueLabel: boolean;
}

interface GridLine {
  y: number;
  value: number;
}

interface SalesChart {
  bars: SalesBar[];
  gridLines: GridLine[];
  baselineY: number;
}

// The chart geometry (dataviz skill: bars ≤24px thick, 4px rounded data-end,
// square at the baseline, hairline gridlines, direct labels only when they
// won't crowd the axis).
const CHART_WIDTH = 640;
const CHART_HEIGHT = 240;
const TOP_PAD = 28;
const BOTTOM_PAD = 30;
const LEFT_PAD = 40;
const RIGHT_PAD = 8;
const PLOT_WIDTH = CHART_WIDTH - LEFT_PAD - RIGHT_PAD;
const PLOT_HEIGHT = CHART_HEIGHT - TOP_PAD - BOTTOM_PAD;
const MAX_BAR_THICKNESS = 24;
const BAR_RADIUS = 4;
// Past this many days, labeling every bar crowds the axis — gridlines and
// the hover tooltip carry the value instead (marks-and-anatomy.md: "label
// selectively, never a number on every point").
const MAX_LABELED_BARS = 14;

// N-C — split out of the former 970-line ReportsDashboardComponent
// verbatim: same chart geometry, same computed signals, same DOM. Only
// the surrounding page chrome (its own route, page-header) is new.
@Component({
  selector: 'app-sales-by-day',
  imports: [DecimalPipe, MatCardModule, MatProgressSpinnerModule, PageHeaderComponent, TranslatePipe],
  templateUrl: './sales-by-day.component.html',
  styleUrl: './sales-by-day.component.scss',
})
export class SalesByDayComponent implements OnInit {
  readonly loading = signal(false);
  readonly salesByDay = signal<SalesByDayDto[]>([]);
  readonly hoveredSalesIndex = signal<number | null>(null);

  readonly salesChart = computed<SalesChart | null>(() => {
    const rows = this.salesByDay();
    if (rows.length === 0) {
      return null;
    }

    const rawMax = Math.max(...rows.map((r) => r.total));
    const { niceMax, step } = this.computeScale(rawMax);
    const barCount = rows.length;
    const slotWidth = PLOT_WIDTH / barCount;
    const barWidth = Math.min(MAX_BAR_THICKNESS, slotWidth - 2);
    const baselineY = TOP_PAD + PLOT_HEIGHT;
    const showValueLabel = barCount <= MAX_LABELED_BARS;

    const bars: SalesBar[] = rows.map((row, i) => {
      const height = niceMax > 0 ? (row.total / niceMax) * PLOT_HEIGHT : 0;
      const x = LEFT_PAD + i * slotWidth + (slotWidth - barWidth) / 2;
      const y = baselineY - height;
      return {
        dateLabel: this.formatDateLabel(row.date, 'short'),
        fullDate: this.formatDateLabel(row.date, 'long'),
        value: row.total,
        path: this.roundedTopBarPath(x, y, barWidth, height, BAR_RADIUS),
        labelX: x + barWidth / 2,
        labelY: y - 6,
        axisLabelX: x + barWidth / 2,
        showValueLabel,
      };
    });

    const gridLines: GridLine[] = [];
    for (let v = 0; v <= niceMax + 1e-9; v += step) {
      gridLines.push({ value: Math.round(v * 100) / 100, y: baselineY - (v / niceMax) * PLOT_HEIGHT });
    }

    return { bars, gridLines, baselineY };
  });

  readonly hoveredSalesBar = computed<SalesBar | null>(() => {
    const index = this.hoveredSalesIndex();
    const chart = this.salesChart();
    return index === null || !chart ? null : chart.bars[index];
  });

  readonly chartWidth = CHART_WIDTH;
  readonly chartHeight = CHART_HEIGHT;
  readonly leftPad = LEFT_PAD;
  readonly rightPad = RIGHT_PAD;

  constructor(private readonly reportingService: ReportingService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.reportingService
      .getSalesByDay()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((sales) => this.salesByDay.set(sales));
  }

  // "YYYY-MM-DD" has no time-of-day or timezone — parsing it with `new
  // Date("YYYY-MM-DD")` reads it as UTC midnight, which can render as the
  // PREVIOUS day once toLocaleDateString formats it in a negative-UTC-offset
  // browser. Splitting the parts and building the Date from y/m/d keeps it
  // in local time, so the calendar day this bucket represents never shifts.
  private formatDateLabel(isoDate: string, style: 'short' | 'long'): string {
    const [year, month, day] = isoDate.split('-').map(Number);
    const date = new Date(year, month - 1, day);
    return date.toLocaleDateString(undefined, style === 'short' ? { month: 'short', day: 'numeric' } : { month: 'long', day: 'numeric', year: 'numeric' });
  }

  private computeScale(rawMax: number, ticks = 4): { niceMax: number; step: number } {
    if (rawMax <= 0) {
      return { niceMax: ticks, step: 1 };
    }
    const rawStep = rawMax / ticks;
    const magnitude = Math.pow(10, Math.floor(Math.log10(rawStep)));
    const residual = rawStep / magnitude;
    const niceResidual = residual <= 1 ? 1 : residual <= 2 ? 2 : residual <= 5 ? 5 : 10;
    const step = niceResidual * magnitude;
    return { niceMax: step * ticks, step };
  }

  // Square at the baseline, rounded only at the data-end (the top) — a plain
  // SVG rect's rx rounds all four corners, so the bar is drawn as a path
  // instead (marks-and-anatomy.md: "4px rounded data-end, square at the
  // baseline"). Radius is clamped so a near-zero bar never draws an arc
  // bigger than the bar itself.
  private roundedTopBarPath(x: number, y: number, width: number, height: number, radius: number): string {
    if (height <= 0) {
      return '';
    }
    const r = Math.min(radius, width / 2, height);
    return [
      `M${x},${y + height}`,
      `L${x},${y + r}`,
      `Q${x},${y} ${x + r},${y}`,
      `L${x + width - r},${y}`,
      `Q${x + width},${y} ${x + width},${y + r}`,
      `L${x + width},${y + height}`,
      'Z',
    ].join(' ');
  }
}
