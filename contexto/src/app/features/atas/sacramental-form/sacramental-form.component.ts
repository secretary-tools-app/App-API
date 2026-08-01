import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzAutocompleteModule } from 'ng-zorro-antd/auto-complete';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';

import { SectionCardComponent } from '../../../shared/components/section-card/section-card.component';
import { TagListInputComponent } from '../../../shared/components/tag-list-input/tag-list-input.component';
import { AtaService, SacramentalService, DiscursantesService } from '../../../core/services';
import { AtaResponse, SacramentalData } from '../../../core/models';

@Component({
  selector: 'app-sacramental-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NzButtonModule,
    NzIconModule,
    NzInputModule,
    NzAutocompleteModule,
    NzSkeletonModule,
    SectionCardComponent,
    TagListInputComponent,
  ],
  template: `
    <header class="page-head page-head--sacramental">
      <button nz-button nzType="text" nzShape="circle" (click)="voltar()" aria-label="Voltar">
        <span nz-icon nzType="arrow-left"></span>
      </button>
      <div class="page-head__title">
        <span class="page-head__eyebrow">
          <span class="tipo-dot tipo-dot--sacramental"></span>
          Ata Sacramental
        </span>
        <h1>{{ dataFormatada() }}</h1>
      </div>
      <button nz-button nzType="text" nzShape="circle" (click)="verPreview()" aria-label="Visualizar" [disabled]="!isEditing()">
        <span nz-icon nzType="eye"></span>
      </button>
    </header>

    @if (loadingInicial()) {
      <div class="loading-box">
        <nz-skeleton [nzActive]="true" [nzParagraph]="{ rows: 8 }"></nz-skeleton>
      </div>
    } @else {
      <form [formGroup]="form" class="form">
        <app-section-card index="01" title="Saudações e boas-vindas" subtitle="Quem preside, dirige e a presença reconhecida" accent="sacramental">
          <input nz-input formControlName="presidido" placeholder="Presidida por" />
          <input nz-input formControlName="dirigido" placeholder="Dirigida por" />
          <div>
            <label class="field-label">Reconhecemos a presença de</label>
            <app-tag-list-input formControlName="reconhecemosPresenca" placeholder="Nome do visitante..."></app-tag-list-input>
          </div>
        </app-section-card>

        <app-section-card index="02" title="Anúncios" accent="sacramental">
          <app-tag-list-input formControlName="anuncios" placeholder="Novo anúncio..."></app-tag-list-input>
        </app-section-card>

        <app-section-card index="03" title="Abertura" subtitle="Recepção, música e oração inicial" accent="sacramental">
          <input nz-input formControlName="recepcionistas" placeholder="Recepcionista" />
          <input nz-input formControlName="pianista" placeholder="Pianista" />
          <input nz-input formControlName="regenteMusica" placeholder="Regente de música" />
          <input nz-input formControlName="hinoAbertura" placeholder="Hino de abertura (nº e nome)" />
          <input nz-input formControlName="oracaoAbertura" placeholder="Oração de abertura" />
        </app-section-card>

        <app-section-card index="04" title="Assuntos da ala" subtitle="Desobrigações, apoios e confirmações" accent="sacramental">
          <div>
            <label class="field-label">Desobrigações</label>
            <app-tag-list-input formControlName="desobrigacoes" placeholder="Nome — chamado"></app-tag-list-input>
          </div>
          <div>
            <label class="field-label">Apoios / novos chamados</label>
            <app-tag-list-input formControlName="apoios" placeholder="Nome — chamado"></app-tag-list-input>
          </div>
          <div>
            <label class="field-label">Confirmações de batismo</label>
            <app-tag-list-input formControlName="confirmacoesBatismo" placeholder="Nome do confirmado"></app-tag-list-input>
          </div>
          <div>
            <label class="field-label">Apoio a membros novos</label>
            <app-tag-list-input formControlName="apoioMembros" placeholder="Nome do membro"></app-tag-list-input>
          </div>
          <div>
            <label class="field-label">Bênção de crianças</label>
            <app-tag-list-input formControlName="bencaoCriancas" placeholder="Nome da criança"></app-tag-list-input>
          </div>
        </app-section-card>

        <app-section-card index="05" title="Sacramento" accent="sacramental">
          <input nz-input formControlName="hinoSacramental" placeholder="Hino sacramental (nº e nome)" />
        </app-section-card>

        <app-section-card index="06" title="Discursantes" subtitle="Oradores e hino intermediário" accent="sacramental">
          <input nz-input formControlName="discursante1" placeholder="Primeiro discursante" [nzAutocomplete]="auto1" (input)="filtrar(1)" />
          <nz-autocomplete #auto1>
            @for (n of sugestoesFiltradas1(); track n) {
              <nz-auto-option [nzValue]="n">{{ n }}</nz-auto-option>
            }
          </nz-autocomplete>
          <input nz-input formControlName="tema1" placeholder="Tema (opcional)" />
          <input nz-input formControlName="obs1" placeholder="Observações (opcional)" />

          <input nz-input formControlName="discursante2" placeholder="Segundo discursante" [nzAutocomplete]="auto2" (input)="filtrar(2)" />
          <nz-autocomplete #auto2>
            @for (n of sugestoesFiltradas2(); track n) {
              <nz-auto-option [nzValue]="n">{{ n }}</nz-auto-option>
            }
          </nz-autocomplete>
          <input nz-input formControlName="tema2" placeholder="Tema (opcional)" />
          <input nz-input formControlName="obs2" placeholder="Observações (opcional)" />

          <input nz-input formControlName="hinoIntermediario" placeholder="Hino intermediário (nº e nome)" />
        </app-section-card>

        <app-section-card index="07" title="Encerramento" subtitle="Último discursante, hino e oração final" accent="sacramental">
          <input nz-input formControlName="ultimoDiscursante" placeholder="Último discursante" [nzAutocomplete]="auto3" (input)="filtrar(3)" />
          <nz-autocomplete #auto3>
            @for (n of sugestoesFiltradas3(); track n) {
              <nz-auto-option [nzValue]="n">{{ n }}</nz-auto-option>
            }
          </nz-autocomplete>
          <input nz-input formControlName="temaUltimo" placeholder="Tema (opcional)" />
          <input nz-input formControlName="obsUltimo" placeholder="Observações (opcional)" />
          <input nz-input formControlName="hinoEncerramento" placeholder="Hino de encerramento (nº e nome)" />
          <input nz-input formControlName="oracaoEncerramento" placeholder="Oração de encerramento" />
          <textarea nz-input formControlName="outros" placeholder="Outras observações da reunião" rows="3"></textarea>
        </app-section-card>
      </form>

      <div class="save-bar">
        <button nz-button nzType="primary" nzSize="large" nzBlock [nzLoading]="saving()" (click)="salvar()">
          {{ isEditing() ? 'Salvar alterações' : 'Salvar ata' }}
        </button>
      </div>
    }
  `,
  styles: [
    `
      .page-head {
        display: flex;
        align-items: center;
        gap: 6px;
        padding: calc(10px + var(--safe-top)) 6px 10px;
        position: sticky;
        top: 0;
        background: var(--paper);
        z-index: 10;
        border-bottom: 1px solid var(--line);
      }
      .page-head__title {
        flex: 1;
        min-width: 0;
      }
      .page-head__eyebrow {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 11.5px;
        font-weight: 600;
        letter-spacing: 0.03em;
        text-transform: uppercase;
        color: var(--accent-sacramental);
      }
      .page-head h1 {
        font-size: 17px;
        line-height: 1.3;
      }

      .loading-box {
        padding: 20px;
      }

      .form {
        padding: 14px 14px 100px;
      }
      .field-label {
        display: block;
        font-size: 12.5px;
        font-weight: 600;
        color: var(--ink-soft);
        margin-bottom: 6px;
      }

      .save-bar {
        position: sticky;
        bottom: 0;
        padding: 12px 16px calc(12px + var(--safe-bottom));
        background: linear-gradient(to top, var(--paper) 60%, transparent);
      }
      .save-bar button {
        border-radius: var(--radius);
        height: 48px;
      }
    `,
  ],
})
export class SacramentalFormComponent implements OnInit {
  ataId!: number;
  ata = signal<AtaResponse | null>(null);
  isEditing = signal(false);
  loadingInicial = signal(true);
  saving = signal(false);

  sugestoes = signal<string[]>([]);
  sugestoesFiltradas1 = signal<string[]>([]);
  sugestoesFiltradas2 = signal<string[]>([]);
  sugestoesFiltradas3 = signal<string[]>([]);

  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private ataService = inject(AtaService);
  private sacramentalService = inject(SacramentalService);
  private discursantesService = inject(DiscursantesService);
  private msg = inject(NzMessageService);

  form = this.fb.group({
    presidido: [''],
    dirigido: [''],
    reconhecemosPresenca: [[] as string[]],
    anuncios: [[] as string[]],
    recepcionistas: [''],
    pianista: [''],
    regenteMusica: [''],
    hinoAbertura: [''],
    oracaoAbertura: [''],
    desobrigacoes: [[] as string[]],
    apoios: [[] as string[]],
    confirmacoesBatismo: [[] as string[]],
    apoioMembros: [[] as string[]],
    bencaoCriancas: [[] as string[]],
    hinoSacramental: [''],
    discursante1: [''],
    tema1: [''],
    obs1: [''],
    discursante2: [''],
    tema2: [''],
    obs2: [''],
    hinoIntermediario: [''],
    ultimoDiscursante: [''],
    temaUltimo: [''],
    obsUltimo: [''],
    hinoEncerramento: [''],
    oracaoEncerramento: [''],
    outros: [''],
  });

  ngOnInit(): void {
    this.ataId = Number(this.route.snapshot.paramMap.get('id'));

    forkJoin({
      ata: this.ataService.getById(this.ataId),
      sac: this.sacramentalService.getByAtaId(this.ataId).pipe(catchError(() => of(null))),
      recentes: this.discursantesService.getRecentes().pipe(catchError(() => of([]))),
    }).subscribe(({ ata, sac, recentes }) => {
      this.ata.set(ata);

      if (sac && (sac.id || sac.ataId)) {
        this.isEditing.set(true);
        this.form.patchValue(sac as any);
      }

      const nomes = new Set<string>();
      for (const r of recentes) {
        if (r.discursante1) nomes.add(r.discursante1);
        if (r.discursante2) nomes.add(r.discursante2);
        if (r.ultimoDiscursante) nomes.add(r.ultimoDiscursante);
      }
      this.sugestoes.set([...nomes]);

      this.loadingInicial.set(false);
    });
  }

  dataFormatada(): string {
    const d = this.ata()?.data;
    if (!d) return '';
    const [y, m, day] = d.split('-');
    return `${day}/${m}/${y}`;
  }

  filtrar(campo: 1 | 2 | 3): void {
    const valor = (
      campo === 1
        ? this.form.controls.discursante1.value
        : campo === 2
        ? this.form.controls.discursante2.value
        : this.form.controls.ultimoDiscursante.value
    )?.toLowerCase() ?? '';

    const filtradas = valor ? this.sugestoes().filter((n) => n.toLowerCase().includes(valor)) : this.sugestoes();

    if (campo === 1) this.sugestoesFiltradas1.set(filtradas);
    if (campo === 2) this.sugestoesFiltradas2.set(filtradas);
    if (campo === 3) this.sugestoesFiltradas3.set(filtradas);
  }

  salvar(): void {
    this.saving.set(true);
    const payload: SacramentalData = { ataId: this.ataId, ...this.form.getRawValue() };

    const req$ = this.isEditing()
      ? this.sacramentalService.update(this.ataId, payload)
      : this.sacramentalService.create(payload);

    req$.subscribe({
      next: () => {
        this.saving.set(false);
        this.msg.success('Ata salva com sucesso.');
        this.router.navigate(['/atas', this.ataId, 'preview']);
      },
      error: () => {
        this.saving.set(false);
        this.msg.error('Não foi possível salvar. Tente novamente.');
      },
    });
  }

  verPreview(): void {
    this.router.navigate(['/atas', this.ataId, 'preview']);
  }

  voltar(): void {
    this.router.navigate(['/atas']);
  }
}
