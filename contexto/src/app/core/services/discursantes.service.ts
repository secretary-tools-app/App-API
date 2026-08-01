import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SacramentalData } from '../models';

@Injectable({ providedIn: 'root' })
export class DiscursantesService {
  private readonly base = `${environment.apiUrl}/discursantes`;

  constructor(private http: HttpClient) {}

  /** Últimos discursantes/temas usados, para sugestão de autocomplete. */
  getRecentes(dias = 90): Observable<SacramentalData[]> {
    return this.http.get<SacramentalData[]>(`${this.base}/recentes`, {
      params: { dias },
    });
  }
}
