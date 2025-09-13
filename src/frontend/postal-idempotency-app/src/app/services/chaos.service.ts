import { Injectable, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, catchError, tap, throwError } from "rxjs";
import { environment } from "../../environments/environment";

export interface ChaosSettings {
  useIdempotencyKey: boolean;
  idempotencyExpirationHours: number;

}

@Injectable({
  providedIn: "root",
})
export class ChaosService {
  reloadSettings() {
    this.loadInitialSettings();
  }
  isSettingsLoaded = signal(false);
  private chaosSettings = signal<ChaosSettings>({
    useIdempotencyKey: true,   
    idempotencyExpirationHours: 24   
  });

  // Public signals for components to react to
  settings = this.chaosSettings.asReadonly();

  constructor(private http: HttpClient) {
    this.loadInitialSettings();
  }

  private loadInitialSettings() {
    this.isSettingsLoaded.set(false);
    this.http
      .get<ChaosSettings>(`${environment.apiUrl}/chaos/settings`)
      .subscribe({
        next: (settings) => {
          this.chaosSettings.set(settings);
          this.isSettingsLoaded.set(true);
        },
        error: (err) => {
          console.error('Failed to load chaos settings from server', err);
          this.isSettingsLoaded.set(true); // כדי שה-ui לא ייתקע
        }
      });
  }

  updateSettings(settings: Partial<ChaosSettings>): void {
    this.chaosSettings.update((current) => ({ ...current, ...settings }));
  }

  updateSettingsOnServer(settings: ChaosSettings): Observable<any> {
    return this.http
      .post(`${environment.apiUrl}/chaos/settings`, settings)
      .pipe(
        tap(() => {
          this.chaosSettings.set(settings);
        }),
        catchError((err) => {
          console.error("Failed to update chaos settings", err);
          // Optionally, revert the local state if the server call fails
          this.loadInitialSettings();
          return throwError(
            () => new Error("Failed to update settings on server")
          );
        })
      );
  }

  /**
   * Generates a UUID v4 for the idempotency key if chaos is enabled.
   * Returns an empty string otherwise, so no key is sent.
   * @param data The data to be sent in the request body.
   */
  async generateIdempotencyKey(data: string): Promise<string> {
    const { useIdempotencyKey } = this.chaosSettings();

    if (!useIdempotencyKey) {
      return ""; // Don't send a key if the feature is disabled
    }   

    // Standard case: generate a valid UUID v4
    return self.crypto.randomUUID();
  }

  /**
   * Get the current idempotency expiration time in hours
   */
  getIdempotencyExpirationHours(): number {
    return this.chaosSettings().idempotencyExpirationHours;
  }

  

}
