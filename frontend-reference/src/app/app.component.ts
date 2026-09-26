import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { RealtimeLeadAlertComponent } from './components/realtime-lead-alert/realtime-lead-alert.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RealtimeLeadAlertComponent],
  template: `
    <router-outlet />
    <app-realtime-lead-alert />
  `
})
export class AppComponent {}
