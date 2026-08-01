import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `
    <div class="app-frame">
      <router-outlet></router-outlet>
    </div>
  `,
  styles: [
    `
      .app-frame {
        min-height: 100dvh;
        max-width: 560px;
        margin: 0 auto;
        background: var(--paper);
        position: relative;

        @media (min-width: 561px) {
          box-shadow: 0 0 0 1px var(--line);
        }
      }
    `,
  ],
})
export class AppComponent {}
