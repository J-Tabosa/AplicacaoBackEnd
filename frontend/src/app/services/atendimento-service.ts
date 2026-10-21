import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Atendimento } from '../models/atendimento';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class AtendimentoService {
  private readonly url = `${API_URL}/Atendimento`;

  constructor(private http: HttpClient) {}

  listar(): Observable<Atendimento[]> {
    return this.http.get<Atendimento[]>(`${this.url}/all`);
  }

  buscar(id: number): Observable<Atendimento> {
    return this.http.get<Atendimento>(`${this.url}/${id}`);
  }

  adicionar(dados: Atendimento): Observable<Atendimento> {
    return this.http.post<Atendimento>(this.url, dados);
  }

  alterar(id: number, dados: Atendimento): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, dados);
  }

  remover(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
