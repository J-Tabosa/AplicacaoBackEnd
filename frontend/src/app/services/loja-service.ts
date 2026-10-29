import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Atendimento } from '../models/atendimento';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class LojaService {
  constructor(private http: HttpClient) {}

  meusPedidos() {
    return this.http.get<Atendimento[]>(`${API_URL}/Atendimento/meus`);
  }
  meuPedido(id: number) {
    return this.http.get<Atendimento>(`${API_URL}/Atendimento/meus/${id}`);
  }
  confirmar(itens: { idItem: number; quantidadeItem: number }[]) {
    return this.http.post<Atendimento>(`${API_URL}/Atendimento/checkout`, { itens });
  }
}
