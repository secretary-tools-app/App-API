import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Cartão de seção numerada. As atas são um documento sequencial
 * (ordem real do culto), então a numeração aqui carrega informação
 * de verdade — não é decoração.
 */
@Component({
  selector: 'app-section-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="section-card" [class.section-card--accent]="accent">
      <header class="section-card__head">
        <span class="section-card__index" [style.color]="accentColor">{{ index }}</span>
        <div>
          <h3 class="section-card__title">{{ title }}</h3>
          @if (subtitle) {
            <p class="section-card__subtitle">{{ subtitle }}</p>
          }
        </div>
      </header>
      <div class="section-card__body">
        <ng-content></ng-content>
      </div>
    </section>
  `,
  styles: [
    `
      .section-card {
        background: var(--paper-raised);
        border: 1px solid var(--line);
        border-radius: var(--radius);
        padding: 18px 16px 20px;
        margin-bottom: 14px;
      }
      .section-card__head {
        display: flex;
        align-items: baseline;
        gap: 10px;
        margin-bottom: 14px;
      }
      .section-card__index {
        font-family: var(--font-display);
        font-size: 13px;
        font-weight: 600;
        letter-spacing: 0.02em;
        opacity: 0.6;
        flex: none;
        min-width: 22px;
      }
      .section-card__title {
        font-family: var(--font-display);
        font-size: 18px;
        font-weight: 600;
        line-height: 1.2;
      }
      .section-card__subtitle {
        margin: 2px 0 0;
        font-size: 12.5px;
        color: var(--ink-soft);
      }
      .section-card__body {
        display: flex;
        flex-direction: column;
        gap: 14px;
      }
    `,
  ],
})
export class SectionCardComponent {
  @Input() index = '01';
  @Input() title = '';
  @Input() subtitle?: string;
  @Input() accent: 'sacramental' | 'batismo' | null = null;

  get accentColor(): string | null {
    if (this.accent === 'sacramental') return 'var(--accent-sacramental)';
    if (this.accent === 'batismo') return 'var(--accent-batismo)';
    return null;
  }
}
