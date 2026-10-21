import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Entrada } from '../models/entrada';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class EntradaService {
  private readonly url = `${API_URL}/EntradaProduto`;

  constructor(private http: HttpClient) {}

  listar(): Observable<Entrada[]> {
    return this.http.get<Entrada[]>(`${this.url}/all`);
  }

  buscar(id: number): Observable<Entrada> {
    return this.http.get<Entrada>(`${this.url}/${id}`);
  }

  adicionar(dados: Entrada): Observable<Entrada> {
    return this.http.post<Entrada>(this.url, dados);
  }

  alterar(id: number, dados: Entrada): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, dados);
  }

  remover(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
