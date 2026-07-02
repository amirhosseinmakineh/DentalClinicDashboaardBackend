import { provideHttpClient } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { provideToastr } from 'ngx-toastr';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(),
    provideAnimations(),
    provideToastr({
      positionClass: 'toast-top-left',
      timeOut: 3500,
      extendedTimeOut: 1200,
      closeButton: true,
      tapToDismiss: true,
      progressBar: true,
      preventDuplicates: true,
      enableHtml: false,
      newestOnTop: true
    })
  ]
};
