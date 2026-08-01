import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzMessageService } from 'ng-zorro-antd/message';
import { AtaService } from '../../../core/services';
import { TipoAta } from '../../../core/models';

@Component({
  selector: 'app-nova-ata',
  standalone: true,
  imports: [CommonModule, FormsModule, NzDatePickerModule, NzButtonModule, NzIconModule],
  template: `
    <header class="page-head">
      <button nz-button nzType="text" nzShape="circle" (click)="voltar()" aria-label="Voltar">
        <span nz-icon nzType="arrow-left"></span>
      </button>
      <h1>Nova ata</h1>
      <span class="page-head__spacer"></span>
    </header>

    <div class="nova-ata">
      <p class="nova-ata__lead">Qual o tipo de reunião? Escolha e informe a data para começar o preenchimento.</p>

      <div class="tipo-grid">
        <button
          type="button"
          class="tipo-card tipo-card--sacramental"
          [class.tipo-card--active]="tipo() === 'sacramental'"
          (click)="tipo.set('sacramental')"
        >
          <span class="tipo-card__badge" nz-icon nzType="file-text"></span>
          <span class="tipo-card__title">Sacramental</span>
          <span class="tipo-card__desc">Reunião sacramental de domingo</span>
        </button>

        <button
          type="button"
          class="tipo-card tipo-card--batismo"
          [class.tipo-card--active]="tipo() === 'batismo'"
          (click)="tipo.set('batismo')"
        >
          <span class="tipo-card__badge" nz-icon nzType="dropbox"></span>
          <span class="tipo-card__title">Batismo</span>
          <span class="tipo-card__desc">Serviço batismal</span>
        </button>
      </div>

      <label class="nova-ata__label">Data</label>
      <nz-date-picker
        [(ngModel)]="data"
        nzSize="large"
        nzFormat="dd/MM/yyyy"
        [nzInputReadOnly]="true"
        nzPlaceHolder="Selecione a data"
        class="nova-ata__date"
      ></nz-date-picker>

      @if (tipo() === 'sacramental' && isSunday() === false) {
        <p class="nova-ata__hint">A reunião sacramental normalmente acontece aos domingos — confirme a data.</p>
      }

      <button
        nz-button
        nzType="primary"
        nzSize="large"
        nzBlock
        class="nova-ata__submit"
        [nzLoading]="loading()"
        [disabled]="!tipo() || !data"
        (click)="continuar()"
      >
        Continuar
      </button>
    </div>
  `,
  styles: [
    `
      .page-head {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: calc(10px + var(--safe-top)) 8px 10px;
        position: sticky;
        top: 0;
        background: var(--paper);
        z-index: 10;
        border-bottom: 1px solid var(--line);
      }
      .page-head h1 {
        font-size: 17px;
      }
      .page-head__spacer {
        flex: 1;
      }

      .nova-ata {
        padding: 20px 20px calc(32px + var(--safe-bottom));
      }
      .nova-ata__lead {
        color: var(--ink-soft);
        font-size: 14.5px;
        line-height: 1.5;
        margin: 0 0 20px;
      }

      .tipo-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 12px;
        margin-bottom: 26px;
      }
      .tipo-card {
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        gap: 8px;
        background: var(--paper-raised);
        border: 1.5px solid var(--line);
        border-radius: var(--radius);
        padding: 18px 14px;
        text-align: left;
        cursor: pointer;
        transition: border-color 0.15s ease, transform 0.1s ease;
        font-family: var(--font-body);

        &:active {
          transform: scale(0.98);
        }
      }
      .tipo-card__badge {
        font-size: 22px;
        width: 40px;
        height: 40px;
        border-radius: 10px;
        display: flex;
        align-items: center;
        justify-content: center;
      }
      .tipo-card--sacramental .tipo-card__badge {
        color: var(--accent-sacramental);
        background: var(--accent-sacramental-soft);
      }
      .tipo-card--batismo .tipo-card__badge {
        color: var(--accent-batismo);
        background: var(--accent-batismo-soft);
      }
      .tipo-card__title {
        font-family: var(--font-display);
        font-weight: 600;
        font-size: 16px;
      }
      .tipo-card__desc {
        font-size: 12.5px;
        color: var(--ink-soft);
        line-height: 1.3;
      }
      .tipo-card--active.tipo-card--sacramental {
        border-color: var(--accent-sacramental);
        box-shadow: 0 0 0 1px var(--accent-sacramental);
      }
      .tipo-card--active.tipo-card--batismo {
        border-color: var(--accent-batismo);
        box-shadow: 0 0 0 1px var(--accent-batismo);
      }

      .nova-ata__label {
        display: block;
        font-size: 13px;
        font-weight: 600;
        color: var(--ink-soft);
        margin-bottom: 6px;
      }
      .nova-ata__date {
        width: 100%;
      }
      .nova-ata__hint {
        font-size: 12.5px;
        color: var(--warning, var(--accent-sacramental));
        margin: 10px 2px 0;
      }
      .nova-ata__submit {
        margin-top: 28px;
        height: 50px;
        font-size: 16px;
        border-radius: var(--radius);
      }
    `,
  ],
})
export class NovaAtaComponent {
  tipo = signal<TipoAta | null>(null);
  data: Date | null = null;
  loading = signal(false);

  constructor(private ataService: AtaService, private router: Router, private msg: NzMessageService) {}

  isSunday(): boolean | null {
    if (!this.data) return null;
    return this.data.getDay() === 0;
  }

  voltar(): void {
    this.router.navigate(['/atas']);
  }

  continuar(): void {
    const tipo = this.tipo();
    if (!tipo || !this.data) return;

    this.loading.set(true);
    const dataStr = this.toIsoDate(this.data);

    this.ataService.create({ tipo, data: dataStr }).subscribe({
      next: (ata) => {
        this.loading.set(false);
        this.router.navigate(['/atas', ata.id, tipo]);
      },
      error: () => {
        this.loading.set(false);
        this.msg.error('Não foi possível criar a ata. Tente novamente.');
      },
    });
  }

  private toIsoDate(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
