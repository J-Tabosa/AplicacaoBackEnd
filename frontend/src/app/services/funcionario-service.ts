import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Funcionario } from '../models/funcionario';
import { API_URL } from './api.config';

@Injectable({ providedIn: 'root' })
export class FuncionarioService {
  private readonly url = `${API_URL}/Funcionario`;

  constructor(private http: HttpClient) {}

  listar(): Observable<Funcionario[]> {
    return this.http.get<Funcionario[]>(`${this.url}/all`);
  }

  buscar(id: number): Observable<Funcionario> {
    return this.http.get<Funcionario>(`${this.url}/${id}`);
  }

  adicionar(dados: Funcionario): Observable<Funcionario> {
    return this.http.post<Funcionario>(this.url, dados);
  }

  alterar(id: number, dados: Funcionario): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, dados);
  }

  remover(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
