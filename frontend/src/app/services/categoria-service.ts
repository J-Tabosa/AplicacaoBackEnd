import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Categoria } from '../models/categoria';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class CategoriaService {
  private readonly url = `${API_URL}/Categoriums`;

  constructor(private http: HttpClient) {}

  listar(): Observable<Categoria[]> {
    return this.http.get<Categoria[]>(`${this.url}`);
  }

  buscar(id: number): Observable<Categoria> {
    return this.http.get<Categoria>(`${this.url}/${id}`);
  }

  adicionar(dados: Categoria): Observable<Categoria> {
    return this.http.post<Categoria>(this.url, dados);
  }

  alterar(id: number, dados: Categoria): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, dados);
  }

  remover(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
