import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzTabsModule } from 'ng-zorro-antd/tabs';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzStatisticModule } from 'ng-zorro-antd/statistic';
import { NzGridModule } from 'ng-zorro-antd/grid';
import { NzMessageService } from 'ng-zorro-antd/message';
// Adicione FormsModule nesta linha
import { FormsModule } from '@angular/forms';


import { ConfiguracoesService } from '../../core/services/configuracoes.service';
import { EstatisticasResponse, UnidadeData, TemplateResponse, SaveTemplateRequest } from '../../core/models/configuracoes.model';

@Component({
  selector: 'app-configuracoes',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, NzButtonModule, NzIconModule, FormsModule,
    NzTabsModule, NzInputModule, NzFormModule, NzStatisticModule, NzGridModule
  ],
  template: `
    <header class="page-head">
      <button nz-button nzType="text" nzShape="circle" (click)="voltar()" aria-label="Voltar">
        <span nz-icon nzType="arrow-left"></span>
      </button>
      <div class="page-head__title">
        <span class="page-head__eyebrow">Ajustes</span>
        <h1>Configurações</h1>
      </div>
      <div style="width: 32px;"></div>
    </header>

    <div class="config-container">
      <nz-tabset nzCentered>
        
        <nz-tab nzTitle="Visão Geral">
           </nz-tab>

        <nz-tab nzTitle="Dados da Ala">
          <div class="tab-content">
            <form nz-form [formGroup]="unidadeForm" (ngSubmit)="salvarUnidade()" nzLayout="vertical">
              <nz-form-item>
                <nz-form-label>Nome da Ala</nz-form-label>
                <nz-form-control>
                  <input nz-input formControlName="nome" />
                </nz-form-control>
              </nz-form-item>
              <nz-form-item>
                <nz-form-label>Bispo</nz-form-label>
                <nz-form-control>
                  <input nz-input formControlName="bispo" />
                </nz-form-control>
              </nz-form-item>
              <nz-form-item>
                <nz-form-label>1º Conselheiro</nz-form-label>
                <nz-form-control>
                  <input nz-input formControlName="primeiroConselheiro" />
                </nz-form-control>
              </nz-form-item>
              <nz-form-item>
                <nz-form-label>2º Conselheiro</nz-form-label>
                <nz-form-control>
                  <input nz-input formControlName="segundoConselheiro" />
                </nz-form-control>
              </nz-form-item>
              <nz-form-item>
                <nz-form-label>Horário</nz-form-label>
                <nz-form-control>
                  <input nz-input formControlName="horario" />
                </nz-form-control>
              </nz-form-item>

              <button nz-button nzType="primary" nzBlock [disabled]="unidadeForm.pristine">
                Salvar Unidade
              </button>
            </form>
          </div>
        </nz-tab>

        <nz-tab nzTitle="Textos Padrão">
          <div class="tab-content" *ngIf="templateAtual() as tpl">
            <p class="help-text">Estes textos aparecerão automaticamente em novas atas.</p>
            
            <div class="template-card">
              <strong>Boas Vindas</strong>
              <textarea nz-input [(ngModel)]="tpl.boasVindas" rows="2"></textarea>
            </div>
            
            <div class="template-card">
              <strong>Sacramento</strong>
              <textarea nz-input [(ngModel)]="tpl.sacramento" rows="2"></textarea>
            </div>
            
            <div class="template-card">
              <strong>Encerramento</strong>
              <textarea nz-input [(ngModel)]="tpl.encerramento" rows="2"></textarea>
            </div>

            <button nz-button nzType="primary" nzBlock (click)="salvarTemplate(tpl)">
                Salvar Templates
            </button>
          </div>
        </nz-tab>

      </nz-tabset>
    </div>
  `,
  styles: [`
    .page-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: calc(16px + var(--safe-top)) 16px 14px;
      background: var(--paper-raised);
      border-bottom: 1px solid var(--line);
    }
    .page-head__title {
      text-align: center;
    }
    .page-head__eyebrow {
      font-size: 11.5px;
      font-weight: 600;
      color: var(--ink-soft);
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }
    .page-head h1 {
      font-size: 20px;
    }
    .config-container {
      padding: 16px;
    }
    .tab-content {
      padding: 16px 8px;
      background: var(--paper-raised);
      border-radius: var(--radius);
      border: 1px solid var(--line);
      margin-top: 8px;
    }
    .help-text {
      color: var(--ink-soft);
      font-size: 13px;
      margin-bottom: 16px;
    }
    .template-card {
      margin-bottom: 16px;
      display: flex;
      flex-direction: column;
      gap: 8px;
      padding-bottom: 16px;
      border-bottom: 1px dashed var(--line);
    }
    .template-card:last-child {
      border-bottom: none;
      margin-bottom: 0;
      padding-bottom: 0;
    }
  `]
})
export class ConfiguracoesComponent implements OnInit {
  private fb = inject(FormBuilder);
  private configService = inject(ConfiguracoesService);
  private router = inject(Router);
  private msg = inject(NzMessageService);

  estatisticas = signal<EstatisticasResponse | null>(null);
  
  // Guardamos apenas o primeiro template para editar
  templateAtual = signal<TemplateResponse | null>(null);
  
  unidadeForm = this.fb.group({
    nome: [''],
    bispo: [''],
    primeiroConselheiro: [''],
    segundoConselheiro: [''],
    recepcionista: [''],
    pianista: [''],
    regenteMusica: [''],
    horario: ['']
  });

  ngOnInit() {
    this.carregarDados();
  }

  carregarDados() {
    this.configService.getEstatisticas().subscribe(res => this.estatisticas.set(res));
    
    this.configService.getUnidade().subscribe(res => {
      if (res) this.unidadeForm.patchValue(res);
    });

    this.configService.getTemplates().subscribe(res => {
        // Pega o primeiro template (geralmente tipo sacramental)
        if (res && res.length > 0) {
            this.templateAtual.set(res[0]);
        }
    });
  }

  salvarUnidade() {
    if (this.unidadeForm.valid) {
      const payload = this.unidadeForm.value as UnidadeData;
      this.configService.saveUnidade(payload).subscribe(() => {
        this.msg.success('Dados da ala atualizados!');
        this.unidadeForm.markAsPristine();
      });
    }
  }

  salvarTemplate(tpl: TemplateResponse) {
    const payload: SaveTemplateRequest = {
        tipoTemplate: tpl.tipoTemplate,
        nome: tpl.nome,
        boasVindas: tpl.boasVindas,
        desobrigacoes: tpl.desobrigacoes,
        apoios: tpl.apoios,
        confirmacoesBatismo: tpl.confirmacoesBatismo,
        apoioMembroNovo: tpl.apoioMembroNovo,
        bencaoCrianca: tpl.bencaoCrianca,
        sacramento: tpl.sacramento,
        mensagens: tpl.mensagens,
        live: tpl.live,
        encerramento: tpl.encerramento
    };

    this.configService.saveTemplate(tpl.id, payload).subscribe(() => {
      this.msg.success('Textos padrões atualizados!');
    });
  }

  voltar() {
    this.router.navigate(['/atas']);
  }
}