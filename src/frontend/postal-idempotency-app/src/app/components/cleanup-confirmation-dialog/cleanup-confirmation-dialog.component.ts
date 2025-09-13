import { Component, Inject, ViewEncapsulation, ChangeDetectionStrategy, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { CleanupPreview } from '../../services/data-cleanup.service';

export interface CleanupConfirmationData {
  preview?: CleanupPreview;
  step: 'first' | 'second';
}

@Component({
  selector: 'app-cleanup-confirmation-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule
  ],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="cleanup-dialog modern-2025" 
         [ngClass]="data.step === 'first' ? 'step-first' : 'step-second'"
         role="alertdialog"
         [attr.aria-labelledby]="'dialog-title-' + data.step"
         [attr.aria-describedby]="'dialog-content-' + data.step">
      
      <!-- Background Glass Morphism Layer -->
      <div class="glass-backdrop" aria-hidden="true"></div>
      
      <!-- Header with Enhanced Micro-interactions -->
      <header class="dialog-header" [attr.id]="'dialog-title-' + data.step">
        <div class="warning-icon-container" 
             [attr.aria-label]="data.step === 'first' ? 'סמל אזהרה קריטית' : 'סמל מחיקה סופית'">
          <div class="icon-pulse-rings" aria-hidden="true"></div>
          <mat-icon class="warning-icon material-symbols" 
                    [attr.aria-hidden]="true">
            {{ data.step === 'first' ? 'warning' : 'delete_forever' }}
          </mat-icon>
          <div class="icon-glow-effect" aria-hidden="true"></div>
        </div>
        
        <h1 class="dialog-title" role="heading" aria-level="1">
          <span class="title-primary">{{ data.step === 'first' ? 'אזהרה קריטית!' : 'אישור סופי למחיקה' }}</span>
          <div class="title-underline" aria-hidden="true"></div>
        </h1>
        
        <p class="dialog-subtitle" role="text">
          <span class="subtitle-icon" aria-hidden="true">{{ data.step === 'first' ? '⚠️' : '🗑️' }}</span>
          {{ getWarningSubtitle() }}
        </p>

      </header>

      <!-- Content with Advanced Accessibility -->
      <main class="dialog-content" [attr.id]="'dialog-content-' + data.step">
        
        <!-- Enhanced Warning Banner -->
        <div class="warning-banner premium-alert" 
             role="alert" 
             aria-live="assertive"
             tabindex="0">
          <div class="alert-icon-wrapper" aria-hidden="true">
            <mat-icon class="alert-icon">error_outline</mat-icon>
            <div class="icon-ripple"></div>
          </div>
          <div class="alert-content">
            <span class="alert-text">לא ניתן לשחזר את הנתונים לאחר המחיקה!</span>
            <div class="alert-emphasis" aria-hidden="true"></div>
          </div>
        </div>

        <!-- First Step: Enhanced Data Types Grid -->
        <section *ngIf="data.step === 'first'" 
                 class="data-types-grid" 
                 role="region"
                 aria-label="סוגי הנתונים שיימחקו">
          <div class="data-type-card hover-lift" 
               *ngFor="let item of getDataTypes(); trackBy: trackByDataType"
               tabindex="0"
               [attr.aria-label]="item.title + ': ' + item.description"
               role="article">
            <div class="card-header" aria-hidden="true">
              <div class="icon-container">
                <mat-icon class="data-icon">{{ item.icon }}</mat-icon>
                <div class="icon-background-glow"></div>
              </div>
              <div class="card-status-indicator"></div>
            </div>
            <div class="card-content">
              <h3 class="item-title" role="heading" aria-level="3">{{ item.title }}</h3>
              <p class="item-description">{{ item.description }}</p>
              <div class="severity-indicator" [attr.aria-label]="'רמת חומרה: ' + item.severity"></div>
            </div>
            <div class="card-hover-overlay" aria-hidden="true"></div>
          </div>
        </section>

        <!-- Second Step: Advanced Stats Dashboard -->
        <section *ngIf="data.step === 'second' && data.preview" 
                 class="deletion-preview-dashboard"
                 role="region"
                 aria-label="סיכום נתונים למחיקה">
          
          <header class="preview-header">
            <h2 class="preview-title" role="heading" aria-level="2">
              <mat-icon aria-hidden="true">assessment</mat-icon>
              פרטי המחיקה המפורטים
            </h2>
            <div class="critical-badge" role="img" aria-label="תווית קריטי">CRITICAL</div>
          </header>

          <div class="stats-grid" role="grid" aria-label="סטטיסטיקות מחיקה">
            <div class="stat-card premium-card" 
                 *ngFor="let stat of getPreviewStats(data.preview); trackBy: trackByStat"
                 role="gridcell"
                 tabindex="0"
                 [attr.aria-label]="stat.label + ': ' + stat.value">
              <div class="stat-icon-wrapper" aria-hidden="true">
                <mat-icon class="stat-icon">{{ stat.icon }}</mat-icon>
                <div class="icon-pulse-ring"></div>
              </div>
              <div class="stat-content">
                <div class="stat-value" [attr.aria-label]="'ערך: ' + stat.value">{{ stat.value }}</div>
                <div class="stat-label">{{ stat.label }}</div>
                <div class="stat-trend" [ngClass]="stat.trend" aria-hidden="true"></div>
              </div>
              <div class="card-accent-line" aria-hidden="true"></div>
            </div>
          </div>

          <div class="final-warning-card" role="alert" aria-live="polite">
            <div class="warning-icon-large" aria-hidden="true">
              <mat-icon>report_problem</mat-icon>
              <div class="danger-pulse"></div>
            </div>
            <div class="warning-content">
              <h3 class="warning-title">אזהרה אחרונה</h3>
              <p class="warning-text">פעולה זו בלתי הפיכה ותמחק את כל הנתונים לצמיתות</p>
            </div>
          </div>
        </section>
      </main>

      <!-- Enhanced Action Buttons -->
      <footer class="dialog-actions premium-actions" role="contentinfo">
        <button type="button"
                mat-stroked-button 
                class="cancel-btn modern-btn"
                (click)="onCancel()"
                [attr.aria-label]="'ביטול הפעולה - מחזיר למסך הקודם'"
                tabindex="0">
          <span class="btn-content">
            <mat-icon aria-hidden="true">close</mat-icon>
            <span class="btn-text">ביטול</span>
          </span>
          <div class="btn-ripple" aria-hidden="true"></div>
        </button>
        
        <button type="button"
                mat-flat-button 
                class="confirm-btn modern-btn danger-btn"
                [ngClass]="data.step === 'first' ? 'step-first' : 'step-second critical-action'"
                (click)="onConfirm()"
                [attr.aria-label]="data.step === 'first' ? 'המשך לשלב הבא' : 'מחיקה סופית - פעולה בלתי הפיכה'"
                tabindex="0">
          <span class="btn-content">
            <mat-icon aria-hidden="true">{{ data.step === 'first' ? 'arrow_forward' : 'delete_forever' }}</mat-icon>
            <span class="btn-text">{{ data.step === 'first' ? 'המשך' : 'מחק הכל' }}</span>
          </span>
          <div class="btn-ripple danger-ripple" aria-hidden="true"></div>
          <div class="btn-glow-effect" aria-hidden="true"></div>
        </button>
      </footer>

      <!-- Screen Reader Only Content -->
      <div class="sr-only" aria-live="polite" [attr.aria-atomic]="true">
        <p *ngIf="data.step === 'first'">
          שלב ראשון מתוך שניים: סקירת נתונים למחיקה. לחץ המשך כדי לעבור לשלב הסופי.
        </p>
        <p *ngIf="data.step === 'second'">
          שלב שני ואחרון: אישור סופי למחיקה. פעולה זו בלתי הפיכה.
        </p>
      </div>
    </div>
  `,
  styleUrls: ['./cleanup-confirmation-dialog.component.scss']
})
export class CleanupConfirmationDialogComponent implements OnInit, OnDestroy {

  getWarningSubtitle(): string {
    // אם הועבר warningText ב-data, השתמש בו, אחרת ברירת מחדל
    if ((this.data as any).warningText) {
      return (this.data as any).warningText;
    }
    if (this.data.step === 'first') {
      return 'פעולה זו תמחק לצמיתות את כל נתוני המדדים';
    }
    return 'זוהי ההזדמנות האחרונה לבטל';
  }
  // Performance optimized data structures
  private readonly dataTypesCache: readonly any[];
  private previewStatsCache: readonly any[] | null = null;

  constructor(
    public dialogRef: MatDialogRef<CleanupConfirmationDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CleanupConfirmationData
  ) {
    // Pre-compute static data for performance
    this.dataTypesCache = Object.freeze([
      Object.freeze({
        icon: 'vpn_key',
        title: 'מפתחות אידמפוטנטיות',
        description: 'כל מפתחות ההגנה מכפילויות',
        severity: 'high'
      }),
      Object.freeze({
        icon: 'insights',
        title: 'מדדים היסטוריים',
        description: 'כל נתוני הביצועים והסטטיסטיקות',
        severity: 'medium'
      }),
      Object.freeze({
        icon: 'history',
        title: 'רישומי פעולות',
        description: 'כל היסטוריית הפעילות במערכת',
        severity: 'high'
      })
    ]);
  }

  ngOnInit(): void {
    // Pre-compute preview stats if available
    if (this.data.preview) {
      this.previewStatsCache = Object.freeze(this.computePreviewStats(this.data.preview));
    }

    // Add dialog entrance animation trigger
    requestAnimationFrame(() => {
      document.body.style.setProperty('--dialog-entrance-delay', '0ms');
    });
  }

  ngOnDestroy(): void {
    // Clean up any custom properties
    document.body.style.removeProperty('--dialog-entrance-delay');
  }

  onCancel(): void {
    // Add exit animation before closing
    this.dialogRef.addPanelClass('dialog-exit');
    setTimeout(() => this.dialogRef.close(false), 200);
  }

  onConfirm(): void {
    // Add confirmation animation
    this.dialogRef.addPanelClass('dialog-confirm');
    setTimeout(() => this.dialogRef.close(true), 150);
  }

  formatDate(dateString: string): string {
    // Optimized date formatting with caching
    try {
      return new Date(dateString).toLocaleDateString('he-IL', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
      });
    } catch {
      return dateString; // Fallback for invalid dates
    }
  }

  // Enhanced functions for 2025 modern UI - Performance Optimized
  getDataTypes(): readonly any[] {
    return this.dataTypesCache; // Return cached immutable data
  }

  getPreviewStats(preview: CleanupPreview): readonly any[] {
    // Return cached stats if available, otherwise compute
    return this.previewStatsCache || Object.freeze(this.computePreviewStats(preview));
  }

  private computePreviewStats(preview: CleanupPreview): any[] {
    return [
      {
        icon: 'vpn_key',
        label: 'רשומות אידמפוטנטיות',
        value: (preview.idempotencyEntriesCount || 0).toLocaleString('he-IL'),
        trend: 'critical'
      },
      {
        icon: 'insights',
        label: 'רשומות מדדים',
        value: (preview.operationMetricsCount || 0).toLocaleString('he-IL'),
        trend: 'warning'
      },
      {
        icon: 'storage',
        label: 'גודל משוער',
        value: `${(preview.estimatedDataSizeKB || 0).toLocaleString('he-IL')} KB`,
        trend: 'info'
      },
      {
        icon: 'schedule',
        label: 'נתונים מתאריך',
        value: this.formatDate(preview.oldestIdempotencyEntry || ''),
        trend: 'neutral'
      }
    ];
  }

  // TrackBy functions for optimal ngFor performance
  trackByDataType(index: number, item: any): string {
    return `${item.icon}-${item.title}`; // Stable identifier
  }

  trackByStat(index: number, item: any): string {
    return `${item.label}-${item.icon}`; // Stable identifier
  }
}
