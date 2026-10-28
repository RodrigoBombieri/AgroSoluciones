import { Component } from '@angular/core';
import { PlayonPanel } from './playon/playon-panel';

@Component({
  selector: 'app-root',
  imports: [PlayonPanel],
  template: '<app-playon-panel />',
})
export class App {}
