import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSkeletonModule } from 'ng-zorro-antd/skeleton';

import { SectionCardComponent } from '../../../shared/components/section-card/section-card.component';
import { AtaService, BatismoService } from '../../../core/services';
import { AtaResponse, BatismoData } from '../../../core/models';

@Component({
  selector: 'app-batismo-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NzButtonModule,
    NzIconModule,
    NzInputModule,
    NzSkeletonModule,
    SectionCardComponent,
  ],
  template: `
    <header class="page-head">
      <button nz-button nzType="text" nzShape="circle" (click)="voltar()" aria-label="Voltar">
        <span nz-icon nzType="arrow-left"></span>
      </button>
      <div class="page-head__title">
        <span class="page-head__eyebrow">
          <span class="tipo-dot tipo-dot--batismo"></span>
          Ata de Batismo
        </span>
        <h1>{{ dataFormatada() }}</h1>
      </div>
      <button nz-button nzType="text" nzShape="circle" (click)="verPreview()" aria-label="Visualizar" [disabled]="!isEditing()">
        <span nz-icon nzType="eye"></span>
      </button>
    </header>

    @if (loadingInicial()) {
      <div class="loading-box">
        <nz-skeleton [nzActive]="true" [nzParagraph]="{ rows: 6 }"></nz-skeleton>
      </div>
    } @else {
      <form [formGroup]="form" class="form">
        <app-section-card index="01" title="Condução do serviço" accent="batismo">
          <input nz-input formControlName="dedicado" placeholder="Dedicado a (opcional)" />
          <input nz-input formControlName="presidido" placeholder="Presidido por" />
          <input nz-input formControlName="dirigido" placeholder="Dirigido por" />
        </app-section-card>

        <app-section-card index="02" title="Batizados" subtitle="Quem foi batizado e por quem" accent="batismo">
          <div formArrayName="batizados" class="batizados-list">
            @for (grupo of batizados.controls; track $index) {
              <div [formGroupName]="$index" class="batizado-row">
                <input nz-input formControlName="nome" placeholder="Nome do batizado" />
                <input nz-input formControlName="batizador" placeholder="Batizado por" />
                <button nz-button nzType="text" nzDanger type="button" (click)="removerBatizado($index)" aria-label="Remover">
                  <span nz-icon nzType="delete"></span>
                </button>
              </div>
            }
          </div>
          <button nz-button nzType="dashed" nzBlock type="button" (click)="adicionarBatizado()">
            <span nz-icon nzType="plus"></span> Adicionar batizado
          </button>
        </app-section-card>

        <app-section-card index="03" title="Testemunhas" accent="batismo">
          <input nz-input formControlName="testemunha1" placeholder="Primeira testemunha" />
          <input nz-input formControlName="testemunha2" placeholder="Segunda testemunha" />
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
        color: var(--accent-batismo);
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
      .batizados-list {
        display: flex;
        flex-direction: column;
        gap: 10px;
        margin-bottom: 10px;
      }
      .batizado-row {
        display: grid;
        grid-template-columns: 1fr 1fr auto;
        gap: 6px;
        align-items: center;
        background: var(--accent-batismo-soft);
        padding: 8px;
        border-radius: 10px;
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
export class BatismoFormComponent implements OnInit {
  ataId!: number;
  ata = signal<AtaResponse | null>(null);
  isEditing = signal(false);
  loadingInicial = signal(true);
  saving = signal(false);

  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private ataService = inject(AtaService);
  private batismoService = inject(BatismoService);
  private msg = inject(NzMessageService);

  form: FormGroup = this.fb.group({
    dedicado: [''],
    presidido: [''],
    dirigido: [''],
    batizados: this.fb.array([]),
    testemunha1: [''],
    testemunha2: [''],
  });

  get batizados(): FormArray {
    return this.form.get('batizados') as FormArray;
  }

  ngOnInit(): void {
    this.ataId = Number(this.route.snapshot.paramMap.get('id'));

    forkJoin({
      ata: this.ataService.getById(this.ataId),
      bat: this.batismoService.getByAtaId(this.ataId).pipe(catchError(() => of(null))),
    }).subscribe(({ ata, bat }) => {
      this.ata.set(ata);

      if (bat && (bat.id || bat.ataId)) {
        this.isEditing.set(true);
        this.form.patchValue({
          dedicado: bat.dedicado,
          presidido: bat.presidido,
          dirigido: bat.dirigido,
          testemunha1: bat.testemunha1,
          testemunha2: bat.testemunha2,
        });
        for (const b of bat.batizados ?? []) {
          this.batizados.push(this.fb.group({ nome: [b.nome], batizador: [b.batizador ?? ''] }));
        }
      }
      if (this.batizados.length === 0) this.adicionarBatizado();

      this.loadingInicial.set(false);
    });
  }

  adicionarBatizado(): void {
    this.batizados.push(this.fb.group({ nome: ['', Validators.required], batizador: [''] }));
  }

  removerBatizado(index: number): void {
    this.batizados.removeAt(index);
  }

  dataFormatada(): string {
    const d = this.ata()?.data;
    if (!d) return '';
    const [y, m, day] = d.split('-');
    return `${day}/${m}/${y}`;
  }

  salvar(): void {
    this.saving.set(true);
    const raw = this.form.getRawValue();
    const payload: BatismoData = {
      ataId: this.ataId,
      dedicado: raw.dedicado,
      presidido: raw.presidido,
      dirigido: raw.dirigido,
      testemunha1: raw.testemunha1,
      testemunha2: raw.testemunha2,
      batizados: (raw.batizados as { nome: string; batizador: string }[]).filter((b) => b.nome?.trim()),
    };

    const req$ = this.isEditing()
      ? this.batismoService.update(this.ataId, payload)
      : this.batismoService.create(payload);

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
