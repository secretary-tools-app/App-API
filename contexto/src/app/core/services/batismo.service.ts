import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BatismoData } from '../models';

@Injectable({ providedIn: 'root' })
export class BatismoService {
  private readonly base = `${environment.apiUrl}/batismo`;

  constructor(private http: HttpClient) {}

  getByAtaId(ataId: number): Observable<BatismoData> {
    return this.http.get<BatismoData>(`${this.base}/${ataId}`);
  }

  create(data: BatismoData): Observable<BatismoData> {
    return this.http.post<BatismoData>(this.base, data);
  }

  update(ataId: number, data: BatismoData): Observable<BatismoData> {
    return this.http.put<BatismoData>(`${this.base}/${ataId}`, data);
  }

  delete(ataId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${ataId}`);
  }
}
