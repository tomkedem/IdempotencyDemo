import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CleanupPreview {
  idempotencyEntriesCount: number;
  operationMetricsCount: number;
  oldestIdempotencyEntry: string;
  oldestMetricEntry: string;
  estimatedDataSizeKB: number;
}

export interface CleanupTokenResponse {
  confirmationToken: string;
  expiresInMinutes: number;
  message: string;
  warning: string;
}

export interface CleanupExecutionResponse {
  message: string;
  timestamp: string;
  warning: string;
}

export interface CleanupRequest {
  confirmationToken: string;
}

@Injectable({
  providedIn: 'root'
})
export class DataCleanupService {
  private readonly apiUrl = `${environment.apiUrl}/DataCleanup`;

  constructor(private http: HttpClient) {}

  /**
   * Generate a confirmation token for cleanup operations
   */
  generateConfirmationToken(): Observable<CleanupTokenResponse> {
    return this.http.post<CleanupTokenResponse>(`${this.apiUrl}/generate-token`, {});
  }

  /**
   * Get preview of what will be deleted
   */
  getCleanupPreview(): Observable<{preview: CleanupPreview, warning: string, recommendation: string}> {
    return this.http.get<{preview: CleanupPreview, warning: string, recommendation: string}>(`${this.apiUrl}/preview`);
  }

  /**
   * Execute complete database cleanup
   */
  executeCompleteCleanup(request: CleanupRequest): Observable<CleanupExecutionResponse> {
    return this.http.post<CleanupExecutionResponse>(`${this.apiUrl}/execute`, request);
  }
}
