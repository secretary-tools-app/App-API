export type TipoAta = 'sacramental' | 'batismo';

export interface CreateAtaRequest {
  tipo: TipoAta;
  data: string; // "YYYY-MM-DD"
}

export interface AtaResponse {
  id: number;
  tipo: TipoAta;
  data: string;
  status: string;
  alaId: number;
}
