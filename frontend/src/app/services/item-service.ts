import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Item } from '../models/item';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class ItemService {
  private readonly url = `${API_URL}/Items`;

  constructor(private http: HttpClient) {}

  listar(
    filtros: { nome?: string; idCategoria?: number; tipo?: string; ativo?: string } = {},
  ): Observable<Item[]> {
    let params = new HttpParams();
    if (filtros.nome) params = params.set('nome', filtros.nome);
    if (filtros.idCategoria) params = params.set('idCategoria', filtros.idCategoria);
    if (filtros.tipo) params = params.set('tipo', filtros.tipo);
    if (filtros.ativo) params = params.set('ativo', filtros.ativo);
    return this.http.get<Item[]>(this.url, { params });
  }

  buscar(id: number): Observable<Item> {
    return this.http.get<Item>(`${this.url}/${id}`);
  }

  adicionar(dados: Item): Observable<Item> {
    return this.http.post<Item>(this.url, dados);
  }

  alterar(id: number, dados: Item): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, dados);
  }

  remover(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
