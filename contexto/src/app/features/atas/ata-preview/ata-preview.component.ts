import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';
import { AtaService, SacramentalService, BatismoService } from '../../../core/services';
import { AtaResponse, SacramentalData, BatismoData } from '../../../core/models';

@Component({
  selector: 'app-ata-preview',
  standalone: true,
  imports: [CommonModule, NzButtonModule, NzIconModule, NzSkeletonModule],
  template: `
    <header class="page-head">
      <button nz-button nzType="text" nzShape="circle" (click)="voltar()" aria-label="Voltar">
        <span nz-icon nzType="arrow-left"></span>
      </button>
      <h1>Visualizar</h1>
      <button nz-button nzType="text" nzShape="circle" (click)="editar()" aria-label="Editar">
        <span nz-icon nzType="edit"></span>
      </button>
    </header>

    @if (loading()) {
      <div class="loading-box"><nz-skeleton [nzActive]="true" [nzParagraph]="{ rows: 10 }"></nz-skeleton></div>
    } @else {
      <article class="doc">
        <h2 class="doc__title">
          Ata {{ ata()?.tipo === 'sacramental' ? 'Sacramental' : 'de Batismo' }} | {{ dataFormatada() }}
        </h2>

        @if (ata()?.tipo === 'sacramental' && sac()) {
          <section class="doc__section">
            <h3>Saudações e boas-vindas</h3>
            <p><strong>Presidida por:</strong> {{ sac()?.presidido || '—' }}</p>
            <p><strong>Dirigida por:</strong> {{ sac()?.dirigido || '—' }}</p>
            @if ((sac()?.reconhecemosPresenca?.length ?? 0) > 0) {
              <p><strong>Reconhecemos a presença de:</strong></p>
              <ul>
                @for (n of sac()?.reconhecemosPresenca; track n) { <li>{{ n }}</li> }
              </ul>
            }
          </section>

          @if ((sac()?.anuncios?.length ?? 0) > 0) {
            <section class="doc__section">
              <h3>Anúncios</h3>
              <ul>
                @for (a of sac()?.anuncios; track a) { <li>{{ a }}</li> }
              </ul>
            </section>
          }

          <section class="doc__section">
            <h3>Abertura</h3>
            <p><strong>Recepcionista:</strong> {{ sac()?.recepcionistas || '—' }}</p>
            <p><strong>Pianista:</strong> {{ sac()?.pianista || '—' }}</p>
            <p><strong>Regente de Música:</strong> {{ sac()?.regenteMusica || '—' }}</p>
            <p><strong>Hino de Abertura:</strong> {{ sac()?.hinoAbertura || '—' }}</p>
            <p><strong>Oração de Abertura:</strong> {{ sac()?.oracaoAbertura || '—' }}</p>
          </section>

          <section class="doc__section">
            <h3>Assuntos da ala</h3>
            @if ((sac()?.desobrigacoes?.length ?? 0) > 0) {
              <p><strong>Desobrigações:</strong></p>
              <ul><li *ngFor="let d of sac()?.desobrigacoes">{{ d }}</li></ul>
            }
            @if ((sac()?.apoios?.length ?? 0) > 0) {
              <p><strong>Apoios:</strong></p>
              <ul><li *ngFor="let a of sac()?.apoios">{{ a }}</li></ul>
            }
            @if ((sac()?.confirmacoesBatismo?.length ?? 0) > 0) {
              <p><strong>Confirmações de batismo:</strong></p>
              <ul><li *ngFor="let c of sac()?.confirmacoesBatismo">{{ c }}</li></ul>
            }
            @if ((sac()?.apoioMembros?.length ?? 0) > 0) {
              <p><strong>Apoio a membros novos:</strong></p>
              <ul><li *ngFor="let a of sac()?.apoioMembros">{{ a }}</li></ul>
            }
            @if ((sac()?.bencaoCriancas?.length ?? 0) > 0) {
              <p><strong>Bênção de crianças:</strong></p>
              <ul><li *ngFor="let b of sac()?.bencaoCriancas">{{ b }}</li></ul>
            }
          </section>

          <section class="doc__section">
            <h3>Sacramento</h3>
            <p><strong>Hino Sacramental:</strong> {{ sac()?.hinoSacramental || '—' }}</p>
          </section>

          <section class="doc__section">
            <h3>Discursantes</h3>
            <p><strong>Primeiro Discursante:</strong> {{ sac()?.discursante1 || '—' }}</p>
            @if (sac()?.tema1) { <p><strong>Tema:</strong> {{ sac()?.tema1 }}</p> }
            <p><strong>Segundo Discursante:</strong> {{ sac()?.discursante2 || '—' }}</p>
            @if (sac()?.tema2) { <p><strong>Tema:</strong> {{ sac()?.tema2 }}</p> }
            <p><strong>Hino Intermediário:</strong> {{ sac()?.hinoIntermediario || '—' }}</p>
          </section>

          <section class="doc__section">
            <h3>Encerramento</h3>
            <p><strong>Último Discursante:</strong> {{ sac()?.ultimoDiscursante || '—' }}</p>
            @if (sac()?.temaUltimo) { <p><strong>Tema:</strong> {{ sac()?.temaUltimo }}</p> }
            <p><strong>Hino de Encerramento:</strong> {{ sac()?.hinoEncerramento || '—' }}</p>
            <p><strong>Oração de Encerramento:</strong> {{ sac()?.oracaoEncerramento || '—' }}</p>
            @if (sac()?.outros) { <p><strong>Observações:</strong> {{ sac()?.outros }}</p> }
          </section>
        }

        @if (ata()?.tipo === 'batismo' && bat()) {
          <section class="doc__section">
            <h3>Condução do serviço</h3>
            @if (bat()?.dedicado) { <p><strong>Dedicado a:</strong> {{ bat()?.dedicado }}</p> }
            <p><strong>Presidido por:</strong> {{ bat()?.presidido || '—' }}</p>
            <p><strong>Dirigido por:</strong> {{ bat()?.dirigido || '—' }}</p>
          </section>

          <section class="doc__section">
            <h3>Batizados</h3>
            <ul>
              @for (b of bat()?.batizados; track b.nome) {
                <li>{{ b.nome }}{{ b.batizador ? ' — batizado(a) por ' + b.batizador : '' }}</li>
              }
            </ul>
          </section>

          <section class="doc__section">
            <h3>Testemunhas</h3>
            <p>{{ bat()?.testemunha1 || '—' }}</p>
            <p>{{ bat()?.testemunha2 || '—' }}</p>
          </section>
        }

        @if ((ata()?.tipo === 'sacramental' && !sac()) || (ata()?.tipo === 'batismo' && !bat())) {
          <p class="doc__empty">Esta ata ainda não foi preenchida.</p>
        }
      </article>
    }
  `,
  styles: [
    `
      .page-head {
        display: flex;
        align-items: center;
        gap: 6px;
        padding: calc(10px + var(--safe-top)) 8px 10px;
        position: sticky;
        top: 0;
        background: var(--paper);
        z-index: 10;
        border-bottom: 1px solid var(--line);
      }
      .page-head h1 {
        font-size: 17px;
        flex: 1;
        text-align: center;
      }
      .loading-box {
        padding: 20px;
      }

      .doc {
        margin: 16px;
        background: var(--paper-raised);
        border: 1px solid var(--line);
        border-radius: var(--radius);
        padding: 24px 20px calc(32px + var(--safe-bottom));
      }
      .doc__title {
        font-size: 19px;
        text-align: center;
        margin-bottom: 20px;
        color: var(--ink);
      }
      .doc__section {
        margin-bottom: 22px;
      }
      .doc__section h3 {
        font-family: var(--font-display);
        font-size: 15px;
        color: var(--ink);
        border-bottom: 1px solid var(--line);
        padding-bottom: 6px;
        margin-bottom: 10px;
      }
      .doc__section p {
        font-size: 14px;
        line-height: 1.55;
        margin: 4px 0;
      }
      .doc__section ul {
        margin: 4px 0 8px;
        padding-left: 20px;
      }
      .doc__section li {
        font-size: 14px;
        line-height: 1.5;
      }
      .doc__empty {
        color: var(--ink-soft);
        text-align: center;
        padding: 20px 0;
      }
    `,
  ],
})
export class AtaPreviewComponent implements OnInit {
  ataId!: number;
  ata = signal<AtaResponse | null>(null);
  sac = signal<SacramentalData | null>(null);
  bat = signal<BatismoData | null>(null);
  loading = signal(true);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private ataService: AtaService,
    private sacramentalService: SacramentalService,
    private batismoService: BatismoService
  ) {}

  ngOnInit(): void {
    this.ataId = Number(this.route.snapshot.paramMap.get('id'));

    this.ataService.getById(this.ataId).subscribe((ata) => {
      this.ata.set(ata);

      if (ata.tipo === 'sacramental') {
        this.sacramentalService
          .getByAtaId(this.ataId)
          .pipe(catchError(() => of(null)))
          .subscribe((detail) => {
            this.sac.set(detail);
            this.loading.set(false);
          });
      } else {
        this.batismoService
          .getByAtaId(this.ataId)
          .pipe(catchError(() => of(null)))
          .subscribe((detail) => {
            this.bat.set(detail);
            this.loading.set(false);
          });
      }
    });
  }

  dataFormatada(): string {
    const d = this.ata()?.data;
    if (!d) return '';
    const [y, m, day] = d.split('-');
    return `${day}/${m}/${y}`;
  }

  editar(): void {
    const tipo = this.ata()?.tipo;
    if (tipo) this.router.navigate(['/atas', this.ataId, tipo]);
  }

  voltar(): void {
    this.router.navigate(['/atas']);
  }
}
