import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TemplateResponse, SaveTemplateRequest, UnidadeData, EstatisticasResponse } from './index';

@Injectable({ providedIn: 'root' })
export class ConfiguracoesService {
  private http = inject(HttpClient);
  private url = `${environment.apiUrl}/configuracoes`;

  getEstatisticas() {
    return this.http.get<EstatisticasResponse>(`${this.url}/estatisticas`);
  }

  getUnidade() {
    return this.http.get<UnidadeData>(`${this.url}/unidade`);
  }

  saveUnidade(data: UnidadeData) {
    return this.http.put<UnidadeData>(`${this.url}/unidade`, data);
  }

  getTemplates() {
    return this.http.get<TemplateResponse[]>(`${this.url}/templates`);
  }
  
  saveTemplate(id: number, data: SaveTemplateRequest) {
    return this.http.put<TemplateResponse>(`${this.url}/templates/${id}`, data);
  }
}