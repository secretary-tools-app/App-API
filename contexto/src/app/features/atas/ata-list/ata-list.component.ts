import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { AtaService, AuthService } from '../../../core/services';
import { AtaResponse } from '../../../core/models';

@Component({
  selector: 'app-ata-list',
  standalone: true,
  imports: [
    CommonModule,
    NzButtonModule,
    NzIconModule,
    NzSkeletonModule,
    NzEmptyModule,
  ],
  template: `
    <header class="page-head">
      <div>
        <span class="page-head__eyebrow">Atas</span>
        <h1>{{ auth.username() }}</h1>
      </div>
      <button
        nz-button
        nzType="text"
        nzShape="circle"
        (click)="configuracoes()"
        aria-label="Configurações"
      >
        <span nz-icon nzType="setting"></span>
      </button>
      <button
        nz-button
        nzType="text"
        nzShape="circle"
        (click)="sair()"
        aria-label="Sair"
      >
        <span nz-icon nzType="logout"></span>
      </button>
    </header>

    <div class="list">
      @if (loading()) {
        <nz-skeleton
          [nzActive]="true"
          [nzParagraph]="{ rows: 3 }"
        ></nz-skeleton>
      } @else if (atas().length === 0) {
        <nz-empty
          nzNotFoundContent="Nenhuma ata ainda. Toque em '+' para criar a primeira."
        ></nz-empty>
      } @else {
        @for (ata of atas(); track ata.id) {
          <button type="button" class="ata-card" (click)="abrir(ata)">
            <span
              class="tipo-dot"
              [class.tipo-dot--sacramental]="ata.tipo === 'sacramental'"
              [class.tipo-dot--batismo]="ata.tipo === 'batismo'"
            ></span>
            <span class="ata-card__info">
              <span class="ata-card__tipo">{{
                ata.tipo === 'sacramental' ? 'Sacramental' : 'Batismo'
              }}</span>
              <span class="ata-card__data">{{ formatar(ata.data) }}</span>
            </span>
            <span class="ata-card__status">{{ ata.status }}</span>
          </button>
        }
      }
    </div>

    <button class="fab" (click)="nova()" aria-label="Nova ata">
      <span nz-icon nzType="plus"></span>
    </button>
  `,
  styles: [
    `
      .page-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: calc(16px + var(--safe-top)) 20px 14px;
      }
      .page-head__eyebrow {
        font-size: 11.5px;
        font-weight: 600;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        color: var(--ink-soft);
      }
      .page-head h1 {
        font-size: 22px;
      }

      .list {
        padding: 0 16px 100px;
        display: flex;
        flex-direction: column;
        gap: 10px;
      }
      .ata-card {
        display: flex;
        align-items: center;
        gap: 12px;
        background: var(--paper-raised);
        border: 1px solid var(--line);
        border-radius: var(--radius);
        padding: 14px 16px;
        text-align: left;
        font-family: var(--font-body);
        cursor: pointer;

        &:active {
          transform: scale(0.99);
        }
      }
      .ata-card__info {
        display: flex;
        flex-direction: column;
        flex: 1;
      }
      .ata-card__tipo {
        font-weight: 600;
        font-size: 14.5px;
      }
      .ata-card__data {
        font-size: 12.5px;
        color: var(--ink-soft);
      }
      .ata-card__status {
        font-size: 11.5px;
        color: var(--ink-soft);
        text-transform: capitalize;
      }

      .fab {
        position: fixed;
        right: 24px;
        bottom: calc(24px + var(--safe-bottom));
        width: 58px;
        height: 58px;
        border-radius: 50%;
        border: none;
        background: var(--ink);
        color: #fff;
        font-size: 22px;
        display: flex;
        align-items: center;
        justify-content: center;
        box-shadow: 0 6px 18px rgba(34, 51, 73, 0.35);
        cursor: pointer;

        &:active {
          transform: scale(0.95);
        }
      }
    `,
  ],
})
export class AtaListComponent implements OnInit {
  atas = signal<AtaResponse[]>([]);
  loading = signal(true);

  constructor(
    private ataService: AtaService,
    public auth: AuthService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.ataService.getAll().subscribe({
      next: (list) => {
        this.atas.set(list);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  configuracoes(): void {
    this.router.navigate(['/configuracoes']);
  }

  formatar(data: string): string {
    const [y, m, d] = data.split('-');
    return `${d}/${m}/${y}`;
  }

  abrir(ata: AtaResponse): void {
    this.router.navigate(['/atas', ata.id, ata.tipo]);
  }

  nova(): void {
    this.router.navigate(['/atas/nova']);
  }

  sair(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
