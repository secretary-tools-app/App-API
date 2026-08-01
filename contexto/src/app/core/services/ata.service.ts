import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AtaResponse, CreateAtaRequest } from '../models';

@Injectable({ providedIn: 'root' })
export class AtaService {
  private readonly base = `${environment.apiUrl}/atas`;

  constructor(private http: HttpClient) {}

  getByMes(mes: string): Observable<AtaResponse[]> {
    return this.http.get<AtaResponse[]>(this.base, { params: { mes } });
  }

  getAll(): Observable<AtaResponse[]> {
    return this.http.get<AtaResponse[]>(`${this.base}/all`);
  }

  getById(id: number): Observable<AtaResponse> {
    return this.http.get<AtaResponse>(`${this.base}/${id}`);
  }

  getByDataTipo(data: string, tipo: string): Observable<AtaResponse | null> {
    return this.http.get<AtaResponse | null>(`${this.base}/by-data`, { params: { data, tipo } });
  }

  create(req: CreateAtaRequest): Observable<AtaResponse> {
    return this.http.post<AtaResponse>(this.base, req);
  }

  update(id: number, req: CreateAtaRequest): Observable<AtaResponse> {
    return this.http.put<AtaResponse>(`${this.base}/${id}`, req);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
