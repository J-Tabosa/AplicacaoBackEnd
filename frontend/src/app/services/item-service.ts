import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Item } from '../models/item';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class ItemService {
  private readonly url = `${API_URL}/Items`;

  constructor(private http: HttpClient) {}

  listar(): Observable<Item[]> {
    return this.http.get<Item[]>(`${this.url}`);
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
