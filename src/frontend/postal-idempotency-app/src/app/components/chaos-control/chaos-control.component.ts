import {
  Component,
  signal,
  computed,
  effect,
  ViewChild,
  ElementRef,
  AfterViewInit,
  OnDestroy,
  HostListener,
  inject,
} from "@angular/core";
import { CommonModule, DatePipe } from "@angular/common";
import { toSignal } from "@angular/core/rxjs-interop";
import { ReactiveFormsModule } from "@angular/forms";
import { HttpClient } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { CleanupConfirmationDialogComponent } from '../cleanup-confirmation-dialog/cleanup-confirmation-dialog.component';
import { DataCleanupService, CleanupPreview } from '../../services/data-cleanup.service';
import { MatSlideToggleModule } from "@angular/material/slide-toggle";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatTooltipModule } from "@angular/material/tooltip";
import {
  interval,
  switchMap,
  timer,
  map,
  startWith,
  Subject,
  takeUntil,
  Observable,
  Subscription,
} from "rxjs";
import { take, finalize } from "rxjs/operators";

import { ChaosService, ChaosSettings } from "../../services/chaos.service";
import { MetricsService, MetricsSummary } from "../../services/metrics.service";
import { ShipmentService } from "../../services/shipment.service";

interface LogEntry {
  timestamp: number;
  level: "info" | "success" | "warn" | "error";
  message: string;
}

@Component({
  selector: "app-chaos-control",
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    ReactiveFormsModule,
    MatSlideToggleModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatTooltipModule,
  ],
  templateUrl: "./chaos-control.component.html",
  styleUrls: ["./chaos-control.component.scss", "./grid-fix.scss"],
})
export class ChaosControlComponent implements AfterViewInit, OnDestroy {
  // --- Progress for Data Deletion ---
  deleteProgress = signal(0); // 0-100
  reloadChaosSettings() {
    this.chaosService.reloadSettings();
  }
  // אפקט קבוע שמעדכן את ה-state המקומי בכל שינוי ב-settings
  private _settingsEffect = effect(() => {
    const currentSettings = this.chaosService.settings();
    if (this.chaosService.isSettingsLoaded()) {
      // עדכון תמידי מהשרת
      this.initialSettings.set({
        useIdempotencyKey: currentSettings.useIdempotencyKey,
        idempotencyExpirationHours: currentSettings.idempotencyExpirationHours,
      });
      this.idempotencyProtectionEnabled.set(currentSettings.useIdempotencyKey);
      this.expirationHours.set(currentSettings.idempotencyExpirationHours);
    }
  });
  isSettingsLoaded;
  // Particle effect state
  particles: Array<{x: number, y: number, delay: number, color: string}> = [];
  // --- Animation State for Data Deletion ---
  showDataDeleteAnimation = signal(false);

  @ViewChild("terminalContent") private terminalContentRef!: ElementRef;
  @ViewChild("performanceChart")
  private chartRef!: ElementRef<HTMLCanvasElement>;
  private destroy$ = new Subject<void>();
  private saveTimeout?: number;
  private chartContext?: CanvasRenderingContext2D;
  private performanceData: {
    timestamp: number;
    successful: number;
    blocked: number;
    chaosErrors: number; // NEW: שגיאות כאשר הגנה כבויה
  }[] = [];

  // Inject services using modern Angular 20 approach
  chaosService = inject(ChaosService); // Made public for template access
  private metricsService = inject(MetricsService);
  private shipmentService = inject(ShipmentService);
  private dataCleanupService = inject(DataCleanupService);
  private dialog = inject(MatDialog);

  // --- State Signals (Angular 20 style) ---
  idempotencyProtectionEnabled = signal(true);
  expirationHours = signal(24);
  private initialSettings = signal<Partial<ChaosSettings>>({});
  metrics = signal<MetricsSummary | null>(null);
  logHistory = signal<LogEntry[]>([]);

  // Simulation properties
  isSimulationRunning = signal(false);
  simulationProgress = signal(0);
  currentClickCount = signal(0);
  private simulationSubscription?: Subscription;

  // --- Derived Signals using computed ---
  isProtectionActive = computed(() => this.idempotencyProtectionEnabled());

  currentTime = toSignal(timer(0, 1000).pipe(map(() => new Date())), {
    initialValue: new Date(),
  });

  settingsChanged = computed(() => {
    const initial = this.initialSettings();
    return (
      this.idempotencyProtectionEnabled() !== initial.useIdempotencyKey ||
      this.expirationHours() !== initial.idempotencyExpirationHours
    );
  });

  systemHealthStatus = computed(() => {
    const m = this.metrics();
    if (!m) return "unknown";

    const successRate = m.successRate;
    const avgResponseTime = m.averageExecutionTimeMs || 0;

    if (successRate >= 98 && avgResponseTime < 200) return "excellent";
    if (successRate >= 95 && avgResponseTime < 500) return "good";
    if (successRate >= 90 && avgResponseTime < 1000) return "fair";
    return "poor";
  });

  constructor() {
    this.isSettingsLoaded = this.chaosService.isSettingsLoaded;
    // Effect to scroll terminal down when new logs are added
    effect(() => {
      if (this.logHistory().length && this.terminalContentRef) {
        this.scrollToBottom();
      }
    });

    // Debug: Check service instance
    console.log('🔧 ChaosControl Constructor - ShipmentService instance:', this.shipmentService);
    console.log('🔧 ChaosControl Constructor - Service ID:', (this.shipmentService as any)._serviceId || 'no-id');

    // Initialize the component
    this.loadInitialSettings();
    this.startRealTimeUpdates();
    this.addLog("info", "מערכת בקרת אידמפוטנטיות הופעלה");
  }

  ngAfterViewInit() {
    setTimeout(() => this.scrollToBottom(), 100);
    this.initializeChart();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();

    // Clear any pending save timeout
    if (this.saveTimeout) {
      clearTimeout(this.saveTimeout);
    }

    // Clear resize timeout
    if (this.resizeTimeout) {
      clearTimeout(this.resizeTimeout);
    }

    // Stop simulation if running
    this.stopSimulation();
  }

  private loadInitialSettings() {
    // טוען מהשרת דרך ה-service (האפקט דואג לעדכן את ה-ui)
    this.chaosService.reloadSettings();
    this.addLog("info", "הגדרות נוכחיות נטענו מהשרת");
  }

  private startRealTimeUpdates() {
    
    // עדכון מדדים כל 3 שניות
    interval(3000)
      .pipe(
        startWith(0),
        switchMap(() => this.metricsService.getMetricsSummary()),
        takeUntil(this.destroy$)
      ) 
      .subscribe({
        next: (metrics) => {
          this.metrics.set(metrics);
          this.updatePerformanceData(metrics);
          
        },
        error: (error) => {
          console.error("שגיאה בטעינת מדדים:", error);
          this.addLog("error", "שגיאה בטעינת מדדים");
        },
      });
      
      
  }

  private updatePerformanceData(metrics: MetricsSummary) {
    const newPoint = {
      timestamp: Date.now(),
      successful: metrics.successfulOperations,
      blocked: metrics.idempotentHits,
      chaosErrors: metrics.chaosDisabledErrors || 0, // NEW: שגיאות כאשר הגנה כבויה
    };

    this.performanceData.push(newPoint);
    this.performanceData = this.performanceData.slice(-10); // Keep last 10 points

    this.drawChart();
  }

  // Methods for template binding
  toggleIdempotencyProtection() {
    this.idempotencyProtectionEnabled.update((current) => !current);
    // Auto-save when user toggles
    this.saveSettings();
  }

  updateExpirationHours(hours: number) {
    this.expirationHours.set(hours);

    // Clear existing timeout
    if (this.saveTimeout) {
      clearTimeout(this.saveTimeout);
    }

    // Set new timeout for auto-save
    this.saveTimeout = window.setTimeout(() => this.saveSettings(), 1000);
  }

  private addLog(level: LogEntry["level"], message: string) {
    const newEntry: LogEntry = {
      timestamp: Date.now(),
      level,
      message,
    };
    this.logHistory.update((currentLogs) =>
      [...currentLogs, newEntry].slice(-50)
    ); // שמירת 50 הרשומות האחרונות
  }

  private scrollToBottom(): void {
    try {
      if (this.terminalContentRef?.nativeElement) {
        this.terminalContentRef.nativeElement.scrollTop =
          this.terminalContentRef.nativeElement.scrollHeight;
      }
    } catch (err) {
      // שגיאה שקטה - לא חשוב אם הגלילה נכשלת
    }
  }

  private saveSettings() {
    const newSettings: Partial<ChaosSettings> = {
      useIdempotencyKey: this.idempotencyProtectionEnabled(),
      idempotencyExpirationHours: this.expirationHours(),
    };

    this.addLog("info", "שומר הגדרות חדשות...");

    // Create a simplified settings object with only the 2 supported settings
    const completeSettings: ChaosSettings = {
      useIdempotencyKey: newSettings.useIdempotencyKey!,
      idempotencyExpirationHours: newSettings.idempotencyExpirationHours!,
    };

    // Update settings on server via HTTP call
    this.chaosService
      .updateSettingsOnServer(completeSettings)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          // Update local state
          this.chaosService.updateSettings(newSettings);
          this.initialSettings.set(newSettings);
          this.addLog("success", "ההגדרות נשמרו בהצלחה בשרת");
        },
        error: (error) => {
          console.error("Failed to save settings:", error);
          this.addLog("error", "שגיאה בשמירת ההגדרות בשרת");
        },
      });
  }

  private initializeChart() {
    if (this.chartRef?.nativeElement) {
      const canvas = this.chartRef.nativeElement;
      
      // Set responsive canvas size based on container
      const container = canvas.parentElement;
      if (container) {
        const containerRect = container.getBoundingClientRect();
        const pixelRatio = window.devicePixelRatio || 1;
        
        // Calculate size excluding margins (0.5rem each side = 1rem total)
        const baseSize = 16; // 1rem in pixels (typical)
        const canvasWidth = Math.max(300, containerRect.width - baseSize);
        const canvasHeight = Math.max(120, containerRect.height - baseSize);
        
        // Set display size
        canvas.style.width = `${canvasWidth}px`;
        canvas.style.height = `${canvasHeight}px`;
        
        // Set actual size for high DPI displays
        canvas.width = canvasWidth * pixelRatio;
        canvas.height = canvasHeight * pixelRatio;
        
        // Scale context for high DPI
        const context = canvas.getContext("2d");
        if (context) {
          context.scale(pixelRatio, pixelRatio);
          this.chartContext = context;
        }
      } else {
        // Fallback to default context
        const context = canvas.getContext("2d");
        this.chartContext = context || undefined;
      }
      
      this.drawChart();
    }
  }

  @HostListener('window:resize', ['$event'])
  onWindowResize() {
    // Debounce resize events
    if (this.resizeTimeout) {
      clearTimeout(this.resizeTimeout);
    }
    this.resizeTimeout = setTimeout(() => {
      this.initializeChart();
    }, 100);
  }

  private resizeTimeout: any;

  private drawChart() {
    if (!this.chartContext) return;

    const canvas = this.chartRef.nativeElement;
    const ctx = this.chartContext;

    // Get display size (not the actual canvas size which is scaled for high DPI)
    const displayWidth = parseInt(canvas.style.width || '360');
    const displayHeight = parseInt(canvas.style.height || '120');

    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Draw grid and axes
    ctx.strokeStyle = "#e2e8f0";
    ctx.lineWidth = 1;

    // Vertical grid lines
    for (let i = 0; i <= 10; i++) {
      const x = (displayWidth / 10) * i;
      ctx.beginPath();
      ctx.moveTo(x, 0);
      ctx.lineTo(x, displayHeight);
      ctx.stroke();
    }

    // Horizontal grid lines
    for (let i = 0; i <= 5; i++) {
      const y = (displayHeight / 5) * i;
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(displayWidth, y);
      ctx.stroke();
    }

    // Draw performance data if available
    if (this.performanceData.length > 1) {
      this.drawPerformanceLine(ctx, displayWidth, displayHeight);
    }
  }

  private drawPerformanceLine(
    ctx: CanvasRenderingContext2D,
    width: number,
    height: number
  ) {
    const maxValue =
      Math.max(
        ...this.performanceData.map((d) => Math.max(d.successful, d.blocked, d.chaosErrors))
      ) || 10;

    // Draw successful operations line
    ctx.strokeStyle = "#22c55e"; // ירוק - פעולות מוצלחות
    ctx.lineWidth = 2;
    ctx.beginPath();

    this.performanceData.forEach((point, index) => {
      const x = (width / (this.performanceData.length - 1)) * index;
      const y = height - (point.successful / maxValue) * height;

      if (index === 0) {
        ctx.moveTo(x, y);
      } else {
        ctx.lineTo(x, y);
      }
    });
    ctx.stroke();

    // Draw blocked operations line
    ctx.strokeStyle = "#ef4444"; // אדום - פעולות חסומות (הגנה עבדה)
    ctx.lineWidth = 2;
    ctx.beginPath();

    this.performanceData.forEach((point, index) => {
      const x = (width / (this.performanceData.length - 1)) * index;
      const y = height - (point.blocked / maxValue) * height;

      if (index === 0) {
        ctx.moveTo(x, y);
      } else {
        ctx.lineTo(x, y);
      }
    });
    ctx.stroke();

    // Draw chaos disabled errors line
    ctx.strokeStyle = "#f97316"; // כתום - שגיאות כאשר הגנה כבויה
    ctx.lineWidth = 2;
    ctx.beginPath();

    this.performanceData.forEach((point, index) => {
      const x = (width / (this.performanceData.length - 1)) * index;
      const y = height - (point.chaosErrors / maxValue) * height;

      if (index === 0) {
        ctx.moveTo(x, y);
      } else {
        ctx.lineTo(x, y);
      }
    });
    ctx.stroke();
  }


  exportMetrics() {
    const currentMetrics = this.metrics();
    if (!currentMetrics) {
      this.addLog("warn", "אין מדדים לייצוא");
      return;
    }

    const reportData = {
      timestamp: new Date().toISOString(),
      metrics: currentMetrics,
      performanceHistory: this.performanceData,
      systemHealth: this.systemHealthStatus(),
      settings: {
        idempotencyEnabled: this.idempotencyProtectionEnabled(),
        expirationHours: this.expirationHours(),
      },
    };

    const blob = new Blob([JSON.stringify(reportData, null, 2)], {
      type: "application/json",
    });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `idempotency-report-${
      new Date().toISOString().split("T")[0]
    }.json`;
    link.click();
    window.URL.revokeObjectURL(url);

    this.addLog("success", "דוח המדדים יוצא בהצלחה");
  }

  // Helper methods for system health indicator
  getSystemHealthClass(): string {
    const status = this.systemHealthStatus();
    return `health-${status}`;
  }

  getSystemHealthIcon(): string {
    const status = this.systemHealthStatus();
    switch (status) {
      case "excellent":
        return "check_circle";
      case "good":
        return "thumb_up";
      case "fair":
        return "warning";
      case "poor":
        return "error";
      default:
        return "help";
    }
  }

  getSystemHealthText(): string {
    const status = this.systemHealthStatus();
    switch (status) {
      case "excellent":
        return "מצוין";
      case "good":
        return "טוב";
      case "fair":
        return "בסדר";
      case "poor":
        return "בעייתי";
      default:
        return "לא ידוע";
    }
  }

  getSystemHealthTrendText(): string {
    const status = this.systemHealthStatus();
    switch (status) {
      case "excellent":
        return "יציב";
      case "good":
        return "תקין";
      case "fair":
        return "מעורער";
      case "poor":
        return "לא יציב";
      default:
        return "לא ידוע";
    }
  }

  getBlockedPercentage(): number {
    const metrics = this.metrics();
    if (!metrics || metrics.totalOperations === 0) return 0;
    return (metrics.idempotentHits / metrics.totalOperations) * 100;
  }

  getSuccessPercentage(): number {
    const metrics = this.metrics();
    if (!metrics || metrics.totalOperations === 0) return 0;
    return (metrics.successfulOperations / metrics.totalOperations) * 100;
  }

  getChaosErrorsPercentage(): number {
    const metrics = this.metrics();
    if (!metrics || metrics.totalOperations === 0) return 0;
    return ((metrics.chaosDisabledErrors || 0) / metrics.totalOperations) * 100;
  }

  getChaosErrorsCount(): number {
    const metrics = this.metrics();
    return metrics?.chaosDisabledErrors || 0;
  }

  getTimeSavedMs(): number {
    const metrics = this.metrics();
    if (!metrics) return 0;
    
    // חישוב זמן שנחסך: מספר החסימות האידמפוטנטיות כפול זמן התגובה הממוצע
    // זה מייצג את הזמן שהיה מבוזבז על פעולות כפולות ללא הגנה
    const blockedOperations = metrics.idempotentHits || 0;
    const avgResponseTime = metrics.averageExecutionTimeMs || 0;
    
    return blockedOperations * avgResponseTime;
  }

  getTimeSavedFormatted(): string {
    const timeSavedMs = this.getTimeSavedMs();
    
    if (timeSavedMs === 0) return '0ms';
    
    // המרה ליחידות זמן מתאימות
    const seconds = timeSavedMs / 1000;
    const minutes = seconds / 60;
    const hours = minutes / 60;
    const days = hours / 24;
    const months = days / 30;
    const years = days / 365;
    
    // בחירת היחידה המתאימה ביותר
    if (years >= 1) {
      return `${years.toFixed(1)} שנים`;
    } else if (months >= 1) {
      return `${months.toFixed(1)} חודשים`;
    } else if (days >= 1) {
      return `${days.toFixed(1)} ימים`;
    } else if (hours >= 1) {
      return `${hours.toFixed(1)} שעות`;
    } else if (minutes >= 1) {
      return `${minutes.toFixed(1)} דקות`;
    } else if (seconds >= 1) {
      return `${seconds.toFixed(1)} שניות`;
    } else {
      return `${timeSavedMs.toFixed(0)}ms`;
    }
  }

  getRecentLogs(): LogEntry[] {
    const logs = this.logHistory();
    // הצגת 10 הרשומות האחרונות בלבד
    return logs.slice(-10).reverse();
  }

  // Simulation methods
  startRandomSimulation(): void {
    if (this.isSimulationRunning()) {
      console.log('🛑 Stop button clicked - stopping simulation');
      this.addLog('warn', 'עוצר סימולציה...');
      this.stopSimulation();
      return;
    }

    // Enhanced debugging for barcode retrieval
    console.log('🔍 === BARCODE DEBUGGING START ===');
    
    // Check current barcode from service
    const currentBarcode = this.shipmentService.currentBarcode();
    console.log('🔍 Current barcode from service signal:', currentBarcode);
    
    // Check localStorage backup
    const localStorageBarcode = localStorage.getItem('lastSearchedBarcode');
    console.log('🔍 Barcode from localStorage:', localStorageBarcode);
    
    // Check service state
    console.log('🔍 ShipmentService complete state:', {
      loading: this.shipmentService.loading(),
      error: this.shipmentService.error(),
      lastResponse: this.shipmentService.lastResponse(),
      currentBarcodeSignal: this.shipmentService.currentBarcode()
    });
    
    // Try to get barcode from either source
    const barcodeToUse = currentBarcode || localStorageBarcode;
    console.log('🔍 Final barcode to use:', barcodeToUse);
    console.log('🔍 === BARCODE DEBUGGING END ===');
    
    if (!barcodeToUse) {
      this.addLog('warn', 'לא נמצא ברקוד פעיל. אנא חפש משלוח תחילה בעמוד "חיפוש משלוח"');
      console.warn('❌ No current barcode available for simulation from any source');
      return;
    }

    console.log('▶️ Start button clicked - starting simulation with barcode:', barcodeToUse);
    this.addLog('info', `מתחיל סימולציה עם ברקוד: ${barcodeToUse}`);
    this.isSimulationRunning.set(true);
    this.simulationProgress.set(0);
    this.currentClickCount.set(0);
    
    // Array of possible delivery statuses with their corresponding IDs
    const statuses = [
      { name: 'delivered', id: 2 },        // נמסר
      { name: 'failed', id: 3 },           // לא נמסר  
      { name: 'partially_delivered', id: 4 } // נמסר חלקי
    ];
    
    // דמוי 50 "תרחישים" של לחיצות ברצף - כל תרחיש הוא 8 לחיצות על אותו סטטוס
    const totalScenarios = 50; // 50 תרחישים
    const clicksPerScenario = 8; // 8 לחיצות בכל תרחיש
    const totalClicks = totalScenarios * clicksPerScenario; // 400 לחיצות סה"כ
    
    console.log(`🎮 Starting realistic simulation - ${totalScenarios} scenarios of ${clicksPerScenario} duplicate clicks each on barcode: ${barcodeToUse}`);
    this.addLog('info', `מתחיל סימולציה ריאליסטית: ${totalScenarios} תרחישים של ${clicksPerScenario} לחיצות כפולות על ברקוד: ${barcodeToUse}`);
    
    // Create sequence of realistic scenarios with human-like random timing
    let currentClickIndex = 0;
    
    const executeNextClick = async () => {
      if (currentClickIndex >= totalClicks || !this.isSimulationRunning()) {
        // Simulation complete or stopped
        this.isSimulationRunning.set(false);
        this.simulationProgress.set(0);
        this.currentClickCount.set(0);
        console.log(`✅ Simulation completed - ${totalScenarios} realistic scenarios executed`);
        this.addLog('success', `סימולציה הושלמה בהצלחה - ${totalScenarios} תרחישים ריאליסטיים בוצעו`);
        return;
      }

      try {
        // Use the barcode we determined at the start of simulation
        const simulationBarcode = barcodeToUse;
        if (!simulationBarcode) {
          console.warn('No barcode available, skipping simulation click');
          return;
        }

        // Calculate which scenario and click within scenario we're on
        const scenarioIndex = Math.floor(currentClickIndex / clicksPerScenario);
        const clickInScenario = currentClickIndex % clicksPerScenario;
        
        // For each new scenario, choose a random status that will be repeated 8 times
        let statusObj: { name: string; id: number };
        if (clickInScenario === 0) {
          // First click of new scenario - choose new random status
          statusObj = statuses[Math.floor(Math.random() * statuses.length)];
          // Store the status for this scenario
          (this as any).currentScenarioStatus = statusObj;
        } else {
          // Subsequent clicks in same scenario - use same status as first click
          statusObj = (this as any).currentScenarioStatus || statuses[0];
        }
        
        // Execute the operation with the same status as the scenario
        await this.shipmentService.updateDeliveryStatus(simulationBarcode, statusObj.id);
        
        // Update progress
        const currentCount = currentClickIndex + 1;
        this.currentClickCount.set(currentCount);
        this.simulationProgress.set((currentCount / totalClicks) * 100);
        
        // Log progress for each new scenario
        if (clickInScenario === 0) {
          console.log(`📊 Starting scenario ${scenarioIndex + 1}/${totalScenarios}: ${clicksPerScenario} clicks on status "${statusObj.name}" (${statusObj.id})`);
          this.addLog('info', `תרחיש ${scenarioIndex + 1}/${totalScenarios}: ${clicksPerScenario} לחיצות על סטטוס "${this.getStatusNameInHebrew(statusObj.id)}"`);
        }
        
        // Log overall progress every 10 scenarios
        if (currentCount % (clicksPerScenario * 10) === 0) {
          const completedScenarios = currentCount / clicksPerScenario;
          console.log(`📈 Progress: ${completedScenarios}/${totalScenarios} scenarios completed`);
          this.addLog('info', `התקדמות: ${completedScenarios}/${totalScenarios} תרחישים הושלמו`);
        }
      } catch (error) {
        console.error('Simulation error:', error);
        // Continue simulation even if individual calls fail
      }
      
      currentClickIndex++;
      
      // Schedule next click with human-like random delay (200-800ms)
      const randomDelay = 200 + Math.random() * 600; // Random between 200-800ms
      setTimeout(executeNextClick, randomDelay);
    };
    
    // Start the first click
    executeNextClick();
  }

  private getStatusNameInHebrew(statusId: number): string {
    switch (statusId) {
      case 2: return 'נמסר';
      case 3: return 'לא נמסר';
      case 4: return 'נמסר חלקי';
      default: return 'לא ידוע';
    }
  }

  private stopSimulation(): void {
    console.log('⏹️ Stopping simulation...');
    
    if (this.simulationSubscription) {
      this.simulationSubscription.unsubscribe();
      this.simulationSubscription = undefined;
      console.log('🗑️ Simulation subscription unsubscribed');
    }
    
    this.isSimulationRunning.set(false);
    this.simulationProgress.set(0);
    this.currentClickCount.set(0);
    
    console.log('✅ Simulation stopped successfully');
    this.addLog('warn', 'סימולציה הופסקה על ידי המשתמש');
  }

  clearLogs(): void {
    this.logHistory.set([]);
    this.addLog("info", "לוג נוקה על ידי המשתמש");
  }

  // Debug method to check barcode state manually
  debugBarcodeState(): void {
    console.log('🔧 === MANUAL BARCODE DEBUG ===');
    console.log('🔧 Service instance:', this.shipmentService);
    console.log('🔧 Service instance ID:', (this.shipmentService as any)._serviceId || 'no-id');
    console.log('🔧 Service constructor name:', this.shipmentService.constructor.name);
    console.log('🔧 Current barcode signal value:', this.shipmentService.currentBarcode());
    console.log('🔧 Service loading:', this.shipmentService.loading());
    console.log('🔧 Service error:', this.shipmentService.error());
    console.log('🔧 Service lastResponse:', this.shipmentService.lastResponse());
    console.log('🔧 localStorage barcode:', localStorage.getItem('lastSearchedBarcode'));
    
    // Check all localStorage keys
    console.log('🔧 All localStorage keys:', Object.keys(localStorage));
    
    // Try to trigger currentBarcode getter directly
    try {
      const currentBarcodeValue = this.shipmentService.currentBarcode();
      console.log('🔧 Direct currentBarcode() call result:', currentBarcodeValue);
    } catch (error) {
      console.error('🔧 Error calling currentBarcode():', error);
    }
    
    console.log('🔧 === END MANUAL DEBUG ===');
    
    const barcodeDisplay = this.shipmentService.currentBarcode() || localStorage.getItem('lastSearchedBarcode') || 'לא נמצא';
    this.addLog('info', `Debug: ברקוד נוכחי = ${barcodeDisplay}, localStorage = ${localStorage.getItem('lastSearchedBarcode')}`);
  }

  // ============= DATA CLEANUP FUNCTIONALITY =============
  
  /**
   * Complete database cleanup with modern dialog and double confirmation
   */
  completeResetWithDbCleanup() {
    // First step - Show warning dialog
    const firstDialogRef = this.dialog.open(CleanupConfirmationDialogComponent, {
      width: '550px',
      maxWidth: '90vw',
      disableClose: true,
      hasBackdrop: true,
      backdropClass: 'cleanup-dialog-backdrop',
      panelClass: 'cleanup-dialog-panel',
      data: {
        step: 'first',
  warningText: 'פעולה זו תמחק לצמיתות את כל נתוני המדדים'
      }
    });

    firstDialogRef.afterClosed().subscribe(firstResult => {
      if (!firstResult) {
        this.addLog('info', 'איפוס המדדים בוטל על ידי המשתמש בשלב הראשון');
        return;
      }

      // Get cleanup preview for second step
      this.addLog('info', '📊 מקבל פרטי איפוס מדדים...');
      this.dataCleanupService.getCleanupPreview().subscribe({
        next: (previewResponse) => {
          this.addLog('success', '✅ פרטי איפוס מדדים התקבלו בהצלחה');
          const preview = previewResponse.preview;
          
          // Second step - Show detailed confirmation
          const secondDialogRef = this.dialog.open(CleanupConfirmationDialogComponent, {
            width: '550px',
            maxWidth: '90vw',
            disableClose: true,
            hasBackdrop: true,
            backdropClass: 'cleanup-dialog-backdrop',
            panelClass: 'cleanup-dialog-panel',
            data: {
              step: 'second',
              preview: preview,
              warningText: 'פעולה זו תמחק לצמיתות את כל נתוני המדדים'
            }
          });

          secondDialogRef.afterClosed().subscribe(secondResult => {
            if (!secondResult) {
              this.addLog('info', 'איפוס המדדים בוטל על ידי המשתמש בשלב השני');
              return;
            }

            // Execute the cleanup
            this.executeCompleteCleanup();
          });
        },
        error: (error) => {
          console.error('Cleanup preview error:', error);
          this.addLog('error', `[ERROR] שגיאה בקבלת פרטי איפוס המדדים: ${error.message || error.statusText || 'שגיאה לא מוכרת'}`);
          
          // You could optionally show a fallback dialog or retry mechanism here
          if (error.status === 0) {
            this.addLog('error', 'השרת לא מגיב - בדוק שהבק-אנד רץ');
          } else if (error.status >= 400 && error.status < 500) {
            this.addLog('error', 'שגיאת לקוח - בדוק את הבקשה');
          } else if (error.status >= 500) {
            this.addLog('error', 'שגיאת שרת - בדוק לוגים בבק-אנד');
          }
        }
      });
    });
  }

  private executeCompleteCleanup() {
    this.addLog('warn', '🔄 מתחיל איפוס נתוני מדדים בלבד...');
    // Generate particles for the animation
    this.particles = Array.from({length: 36}, (_, i) => ({
      x: 40 + Math.random() * 20 - 10, // spread around center
      y: 60 + Math.random() * 10 - 5,  // start near bin opening
      delay: Math.floor(Math.random() * 800),
      color: `linear-gradient(135deg, #00ffe7 60%, ${['#fff','#ff00e7','#ffb300','#00bfff'][i%4]} 100%)`
    }));

    // Show futuristic data deletion animation overlay
    this.showDataDeleteAnimation.set(true);
    this.deleteProgress.set(0);
    const animationMinDuration = 5000; // 5 שניות לפחות
    const animationStart = Date.now();

    // סימולציית התקדמות מחיקה (אם אין API אמיתי)
    const progressInterval = setInterval(() => {
      const current = this.deleteProgress();
      if (current < 90) {
        this.deleteProgress.set(current + Math.floor(Math.random() * 7) + 2); // קפיצות אקראיות
      } else {
        clearInterval(progressInterval);
      }
    }, 350);

    // Generate confirmation token
    this.dataCleanupService.generateConfirmationToken().subscribe({
      next: (tokenResponse) => {
        this.addLog('info', `🔑 נוצר טוקן אישור (תוקף: ${tokenResponse.expiresInMinutes} דקות)`);

        // Execute cleanup with token
        this.dataCleanupService.executeCompleteCleanup({ 
          confirmationToken: tokenResponse.confirmationToken 
        }).subscribe({
          next: (response) => {
            // Reset in-memory metrics
            this.performanceData = [];
            this.drawChart();
            // Ensure animation is shown at least 2 seconds
            const elapsed = Date.now() - animationStart;
            const remaining = Math.max(animationMinDuration - elapsed, 0);
            setTimeout(() => {
              this.deleteProgress.set(100);
              setTimeout(() => {
                this.showDataDeleteAnimation.set(false);
                this.particles = [];
                this.deleteProgress.set(0);
              }, 800); // השהיה קצרה לסיום
            }, remaining);
            this.addLog('success', '✅ איפוס נתוני המדדים הושלם בהצלחה!');
            this.addLog('success', response.message);
            this.addLog('warn', '⚠️ רק נתוני המדדים נמחקו. שאר הנתונים לא נפגעו.');
          },
          error: (error) => {
            clearInterval(progressInterval);
            this.deleteProgress.set(0);
            console.error('Cleanup execution error:', error);
            this.addLog('error', `❌ שגיאה באיפוס נתוני המדדים: ${error.error?.error || error.message}`);
          }
        });
      },
      error: (error) => {
        clearInterval(progressInterval);
        this.deleteProgress.set(0);
        console.error('Token generation error:', error);
        this.addLog('error', '❌ שגיאה ביצירת טוקן אישור');
      }
    });
  }
}
