// src/app/core/models/configuracoes.model.ts

export interface TemplateResponse {
  id: number;
  alaId: number;
  tipoTemplate: number;
  nome: string;
  boasVindas: string;
  desobrigacoes: string;
  apoios?: string;
  confirmacoesBatismo: string;
  apoioMembroNovo: string;
  bencaoCrianca: string;
  sacramento: string;
  mensagens: string;
  live: string;
  encerramento: string;
}

// O Request espelha o TemplateResponse (sem ID e alaId)
export type SaveTemplateRequest = Omit<TemplateResponse, 'id' | 'alaId'>;

export interface UnidadeData {
  nome?: string;
  bispo?: string;
  primeiroConselheiro?: string;
  segundoConselheiro?: string;
  recepcionista?: string;
  pianista?: string;
  regenteMusica?: string;
  horario?: string;
}

export interface EstatisticasResponse {
  totalAtas: number;
  atasSacramentais: number;
  atasBatismo: number;
  atasMesAtual: number;
}