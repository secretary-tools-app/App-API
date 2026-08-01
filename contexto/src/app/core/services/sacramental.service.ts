import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SacramentalData } from '../models';

@Injectable({ providedIn: 'root' })
export class SacramentalService {
  private readonly base = `${environment.apiUrl}/sacramental`;

  constructor(private http: HttpClient) {}

  getByAtaId(ataId: number): Observable<SacramentalData> {
    return this.http.get<SacramentalData>(`${this.base}/${ataId}`);
  }

  create(data: SacramentalData): Observable<SacramentalData> {
    return this.http.post<SacramentalData>(this.base, data);
  }

  update(ataId: number, data: SacramentalData): Observable<SacramentalData> {
    return this.http.put<SacramentalData>(`${this.base}/${ataId}`, data);
  }

  delete(ataId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${ataId}`);
  }
}
