import {
  ApplicationConfig,
  provideZonelessChangeDetection, // Corrected function name
} from "@angular/core";
import { provideRouter } from "@angular/router";

import { routes } from "./app.routes";
import { provideAnimationsAsync } from "@angular/platform-browser/animations/async";
import { provideHttpClient } from "@angular/common/http";

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(), // Corrected function name
    provideRouter(routes),
    provideAnimationsAsync(),
    provideHttpClient(),
  ],
};
