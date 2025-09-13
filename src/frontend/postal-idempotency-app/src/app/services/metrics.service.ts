import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, interval } from "rxjs";
import { switchMap, shareReplay } from "rxjs/operators";
import { environment } from "../../environments/environment";

export interface MetricsSummary {
  totalOperations: number;
  idempotentHits: number;
  successfulOperations: number;   
  chaosDisabledErrors: number; // NEW: שגיאות כאשר הגנה כבויה
  averageExecutionTimeMs: number;

  successRate: number;
  lastUpdated: string;
  systemHealth: string;
  throughputPerMinute: number;
  peakResponseTime: number;
  minResponseTime: number;

}

export interface RealTimeMetrics {
  currentResponseTime: number;
  operationsPerSecond: number;
  systemLoad: number;
  memoryUsage: number;
  activeConnections: number;
  uptime: string;
  healthStatus: string;

  // Add missing properties that the template expects
  totalOperations: number;
  successfulOperations: number;
  idempotentBlocks: number;
  averageResponseTime: number;
  successRate: number;
  peakResponseTime: number;
  minResponseTime: number;
  failedOperations: number;
}

export interface SystemHealth {
  status: string;
  uptime: string;
  memoryUsage: number;
  operationsPerSecond: number;
  systemLoad: number;
  responseTime: {
    current: number;
    average: number;
    peak: number;
    min: number;
  };
  operations: {
    total: number;
    successful: number;
    errors: number;
    idempotentBlocks: number;
    successRate: number;
  };
  timestamp: string;

  // Add missing properties that the component expects
  isHealthy: boolean;
  performanceLevel: string;
}

@Injectable({
  providedIn: "root",
})
export class MetricsService {
  private apiUrl = `${environment.apiUrl}/Metrics`;

  constructor(private http: HttpClient) {}

  getMetricsSummary(): Observable<MetricsSummary> {
    return this.http.get<MetricsSummary>(`${this.apiUrl}/summary`);
  }

  getRealTimeMetrics(): Observable<RealTimeMetrics> {
    return this.http.get<RealTimeMetrics>(`${this.apiUrl}/realtime`);
  }

  getSystemHealth(): Observable<SystemHealth> {
    return this.http.get<SystemHealth>(`${this.apiUrl}/health`);
  }

  resetMetrics(): Observable<any> {
    return this.http.post(`${this.apiUrl}/reset`, {});
  }

  // Real-time streaming metrics (polls every 2 seconds)
  getRealTimeMetricsStream(): Observable<RealTimeMetrics> {
    return interval(2000).pipe(
      switchMap(() => this.getRealTimeMetrics()),
      shareReplay(1)
    );
  }

  // System health streaming (polls every 5 seconds)
  getSystemHealthStream(): Observable<SystemHealth> {
    return interval(5000).pipe(
      switchMap(() => this.getSystemHealth()),
      shareReplay(1)
    );
  }
}
