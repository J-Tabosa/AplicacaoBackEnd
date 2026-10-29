import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Estoque } from '../models/estoque';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class EstoqueService {
  constructor(private http: HttpClient) {}
  listar() {
    return this.http.get<Estoque[]>(`${API_URL}/Estoque`);
  }
  buscar(id: number) {
    return this.http.get<Estoque>(`${API_URL}/Estoque/${id}`);
  }
}
