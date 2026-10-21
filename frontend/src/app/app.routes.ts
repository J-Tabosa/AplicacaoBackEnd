import { Routes } from '@angular/router';
import { HomeComponent } from './components/home/home';
import { PessoasComponent } from './components/pessoas/pessoas';
import { CategoriasComponent } from './components/categorias/categorias';
import { ItensComponent } from './components/itens/itens';
import { EntradasComponent } from './components/entradas/entradas';
import { AtendimentosComponent } from './components/atendimentos/atendimentos';

export const routes: Routes = [
  { path: '', redirectTo: 'inicio', pathMatch: 'full' },
  { path: 'inicio', component: HomeComponent },
  { path: 'pessoas', component: PessoasComponent, data: { funcionario: false } },
  { path: 'funcionarios', component: PessoasComponent, data: { funcionario: true } },
  { path: 'categorias', component: CategoriasComponent },
  { path: 'itens', component: ItensComponent },
  { path: 'entradas', component: EntradasComponent },
  { path: 'atendimentos', component: AtendimentosComponent },
  { path: '**', redirectTo: 'inicio' },
];
