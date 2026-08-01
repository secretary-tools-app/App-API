export interface BatizadoItem {
  nome: string;
  batizador?: string | null;
}

export interface ProgramaBatismo {
  preludio?: string | null;
  boasVindasPor?: string | null;
  hinoAbertura?: string | null;
  oracaoAbertura?: string | null;
  mensagens?: string[] | null;
  apresentacaoMusical?: string | null;
  temEspera: boolean;
  hinosEspera?: string[] | null;
  batizados?: BatizadoItem[] | null;
  confirmacoes?: string[] | null;
  testemunhosNovos?: string | null;
  hinoEncerramento?: string | null;
  oracaoEncerramento?: string | null;
  posludio?: string | null;
  observacoes?: string | null;
}

export interface BatismoData {
  id?: number;
  ataId: number;
  dedicado?: string | null;
  presidido?: string | null;
  dirigido?: string | null;
  batizados?: BatizadoItem[] | null;
  testemunha1?: string | null;
  testemunha2?: string | null;
  programa?: ProgramaBatismo | null;
}
