import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { Sessao } from '../models/sessao';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class SessaoService {
  atual = signal<Sessao | null>(null);
  constructor(private http: HttpClient) {}

  verificar() {
    return this.http.get<Sessao>(`${API_URL}/Sessao`).pipe(tap((sessao) => this.atual.set(sessao)));
  }

  identificar(cpf: number, dataNascimento: string) {
    return this.http
      .post<Sessao>(`${API_URL}/Sessao`, { cpf, dataNascimento })
      .pipe(tap((sessao) => this.atual.set(sessao)));
  }

  sair() {
    return this.http.delete<void>(`${API_URL}/Sessao`).pipe(tap(() => this.atual.set(null)));
  }
}
