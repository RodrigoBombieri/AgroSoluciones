import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import localeEsAr from '@angular/common/locales/es-AR';
import { xsrfInterceptor } from './core/xsrf.interceptor';

// Números y fechas con formato argentino (45.120 kg)
registerLocaleData(localeEsAr);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withFetch(), withInterceptors([xsrfInterceptor])),
    { provide: LOCALE_ID, useValue: 'es-AR' },
  ],
};
