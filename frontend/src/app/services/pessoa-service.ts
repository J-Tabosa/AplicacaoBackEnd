import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Pessoa } from '../models/pessoa';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class PessoaService {
  private readonly url = `${API_URL}/PessoaEndereco`;

  constructor(private http: HttpClient) {}

  listar(): Observable<Pessoa[]> {
    return this.http.get<Pessoa[]>(`${this.url}/all`);
  }

  buscar(id: number): Observable<Pessoa> {
    return this.http.get<Pessoa>(`${this.url}/${id}`);
  }

  adicionar(dados: Pessoa): Observable<Pessoa> {
    return this.http.post<Pessoa>(this.url, dados);
  }

  alterar(id: number, dados: Pessoa): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, dados);
  }

  remover(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
