import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, forkJoin } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { emptyPage, PagedResult } from '../../../shared/models/pagination.models';
import { PurchaseOrderAgingLineDto } from '../../../shared/models/purchasing.models';
import {
  CashierPerformanceDto,
  SalesByDayDto,
  SalesLedgerEntryDto,
  StockLevelRecordDto,
  StockMovementRecordDto,
  TopSellingItemDto,
} from '../../../shared/models/reporting.models';
import { InventoryValuationLineDto } from '../../../shared/models/warehouse.models';
import { UserDto } from '../../../shared/models/users.models';
import { PurchasingService } from '../../purchasing/purchasing.service';
import { UsersService } from '../../users/users.service';
import { WarehouseService } from '../../warehouse/warehouse.service';
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

@Component({
  selector: 'app-reports-dashboard',
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    TranslatePipe,
  ],
  templateUrl: './reports-dashboard.component.html',
  styleUrl: './reports-dashboard.component.scss',
})
export class ReportsDashboardComponent implements OnInit {
  readonly loading = signal(false);
  readonly salesByDay = signal<SalesByDayDto[]>([]);
  readonly topSelling = signal<TopSellingItemDto[]>([]);
  readonly lowStock = signal<StockLevelRecordDto[]>([]);

  readonly hoveredSalesIndex = signal<number | null>(null);

  readonly takeControl = new FormControl(10, { nonNullable: true });

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

  readonly topSellingMax = computed(() => Math.max(...this.topSelling().map((r) => r.totalRevenue), 0));

  // J — the five new reports. salesLedger/cashierPerformance/stockMovements
  // share one date-range filter (dateRangeForm); inventoryValuation/
  // purchaseOrderAging are live current-state snapshots with no date axis
  // to filter on.
  readonly salesLedger = signal<PagedResult<SalesLedgerEntryDto>>(emptyPage());
  readonly cashierPerformance = signal<CashierPerformanceDto[]>([]);
  readonly stockMovements = signal<PagedResult<StockMovementRecordDto>>(emptyPage());
  readonly inventoryValuation = signal<InventoryValuationLineDto[]>([]);
  readonly purchaseOrderAging = signal<PurchaseOrderAgingLineDto[]>([]);
  readonly loadingDateFiltered = signal(false);
  readonly loadingValuation = signal(false);
  readonly loadingAging = signal(false);

  // CashierUserId -> display name. Best-effort: UsersController is
  // Admin-only (H), but this dashboard is also open to Manager (see
  // REPORTS_ROLES) — a Manager viewing this report simply falls back to
  // "User #N" rather than the whole dashboard failing to load over one
  // 403 on a name lookup that isn't essential to the report itself.
  readonly cashierNames = signal<Map<number, string>>(new Map());

  readonly dateRangeForm = new FormGroup({
    fromDate: new FormControl('', { nonNullable: true }),
    toDate: new FormControl('', { nonNullable: true }),
  });

  readonly inventoryValuationTotal = computed(() => this.inventoryValuation().reduce((sum, l) => sum + l.totalValue, 0));

  constructor(
    private readonly reportingService: ReportingService,
    private readonly warehouseService: WarehouseService,
    private readonly purchasingService: PurchasingService,
    private readonly usersService: UsersService,
  ) {}

  ngOnInit(): void {
    this.loadAll();
    this.loadDateFilteredReports();
    this.loadInventoryValuation();
    this.loadPurchaseOrderAging();
    this.usersService.getUsers(1, 100).subscribe({
      next: (result) => this.cashierNames.set(new Map(result.items.map((u: UserDto) => [u.id, u.userName]))),
      error: () => this.cashierNames.set(new Map()),
    });
    this.takeControl.valueChanges.subscribe(() => this.loadTopSelling());
  }

  private loadAll(): void {
    this.loading.set(true);
    forkJoin({
      sales: this.reportingService.getSalesByDay(),
      top: this.reportingService.getTopSellingItems(this.takeControl.value),
      lowStock: this.reportingService.getLowStock(),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(({ sales, top, lowStock }) => {
        this.salesByDay.set(sales);
        this.topSelling.set(top);
        this.lowStock.set(lowStock);
      });
  }

  private loadTopSelling(): void {
    this.reportingService.getTopSellingItems(this.takeControl.value).subscribe({
      next: (top) => this.topSelling.set(top),
    });
  }

  // fromDate/toDate are native <input type="date"> strings ("YYYY-MM-DD")
  // in the browser's own local time — converted to UTC ISO instants here,
  // the same new Date(...).toISOString() idiom items-admin's own
  // promotion dates already use. toDate is treated as the END of that
  // calendar day, not its start, so "filter through today" actually
  // includes today's sales.
  private dateRangeUtc(): { fromUtc?: string; toUtc?: string } {
    const { fromDate, toDate } = this.dateRangeForm.getRawValue();
    return {
      fromUtc: fromDate ? new Date(`${fromDate}T00:00:00`).toISOString() : undefined,
      toUtc: toDate ? new Date(`${toDate}T23:59:59.999`).toISOString() : undefined,
    };
  }

  onDateRangeChange(): void {
    this.loadDateFilteredReports();
  }

  private loadDateFilteredReports(page = 1): void {
    const { fromUtc, toUtc } = this.dateRangeUtc();
    this.loadingDateFiltered.set(true);
    forkJoin({
      ledger: this.reportingService.getSalesLedger(page, this.salesLedger().pageSize, fromUtc, toUtc),
      performance: this.reportingService.getCashierPerformance(fromUtc, toUtc),
      movements: this.reportingService.getStockMovements(1, this.stockMovements().pageSize, fromUtc, toUtc),
    })
      .pipe(finalize(() => this.loadingDateFiltered.set(false)))
      .subscribe(({ ledger, performance, movements }) => {
        this.salesLedger.set(ledger);
        this.cashierPerformance.set(performance);
        this.stockMovements.set(movements);
      });
  }

  onSalesLedgerPageChange(event: PageEvent): void {
    if (event.pageSize !== this.salesLedger().pageSize) {
      this.salesLedger.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    const { fromUtc, toUtc } = this.dateRangeUtc();
    this.reportingService.getSalesLedger(event.pageIndex + 1, this.salesLedger().pageSize, fromUtc, toUtc).subscribe({
      next: (result) => this.salesLedger.set(result),
    });
  }

  onStockMovementsPageChange(event: PageEvent): void {
    if (event.pageSize !== this.stockMovements().pageSize) {
      this.stockMovements.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    const { fromUtc, toUtc } = this.dateRangeUtc();
    this.reportingService.getStockMovements(event.pageIndex + 1, this.stockMovements().pageSize, fromUtc, toUtc).subscribe({
      next: (result) => this.stockMovements.set(result),
    });
  }

  private loadInventoryValuation(): void {
    this.loadingValuation.set(true);
    this.warehouseService
      .getInventoryValuation()
      .pipe(finalize(() => this.loadingValuation.set(false)))
      .subscribe({ next: (lines) => this.inventoryValuation.set(lines) });
  }

  private loadPurchaseOrderAging(): void {
    this.loadingAging.set(true);
    this.purchasingService
      .getPurchaseOrderAging()
      .pipe(finalize(() => this.loadingAging.set(false)))
      .subscribe({ next: (lines) => this.purchaseOrderAging.set(lines) });
  }

  cashierName(cashierUserId: number): string {
    return this.cashierNames().get(cashierUserId) ?? `#${cashierUserId}`;
  }

  // Only Ordered/PartiallyReceived orders carry an age at all — see
  // PurchaseOrderAgingLineDto's own comment.
  openPurchaseOrderAging(): PurchaseOrderAgingLineDto[] {
    return this.purchaseOrderAging().filter((o) => o.ageDaysSinceOrdered !== null);
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

  // Every row GetLowStock returns is already at/below its own threshold —
  // the split here is just how far below: zero on hand is a harder stop
  // (can't sell it at all) than merely-below-threshold, so it earns the
  // more severe status step. Never color alone (palette.md) — the icon and
  // the "Out of stock"/"Low stock" text always ride along with it.
  stockSeverity(row: StockLevelRecordDto): 'critical' | 'warning' {
    return row.quantityOnHand <= 0 ? 'critical' : 'warning';
  }
}
