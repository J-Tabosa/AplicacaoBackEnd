import { Routes } from '@angular/router';
import { HomeComponent } from './components/home/home';
import { PessoasComponent } from './components/pessoas/pessoas';
import { CategoriasComponent } from './components/categorias/categorias';
import { ItensComponent } from './components/itens/itens';
import { EntradasComponent } from './components/entradas/entradas';
import { AtendimentosComponent } from './components/atendimentos/atendimentos';
import { EstoqueComponent } from './components/estoque/estoque';
import { LojaComponent } from './components/loja/loja';
import { ItemLojaComponent } from './components/item-loja/item-loja';
import { IdentificacaoComponent } from './components/identificacao/identificacao';
import { CadastroClienteComponent } from './components/cadastro-cliente/cadastro-cliente';
import { CarrinhoComponent } from './components/carrinho/carrinho';
import { MeusPedidosComponent } from './components/meus-pedidos/meus-pedidos';
import { acessoEquipe } from './services/acesso-guard';

export const routes: Routes = [
  { path: '', redirectTo: 'loja', pathMatch: 'full' },
  { path: 'loja', component: LojaComponent },
  { path: 'loja/itens/:id', component: ItemLojaComponent },
  { path: 'loja/identificar', component: IdentificacaoComponent },
  { path: 'loja/cadastro', component: CadastroClienteComponent },
  { path: 'loja/carrinho', component: CarrinhoComponent },
  { path: 'loja/pedidos', component: MeusPedidosComponent },
  { path: 'loja/pedidos/:id', component: MeusPedidosComponent },
  { path: 'inicio', component: HomeComponent, canActivate: [acessoEquipe] },
  {
    path: 'pessoas',
    component: PessoasComponent,
    data: { funcionario: false },
    canActivate: [acessoEquipe],
  },
  {
    path: 'funcionarios',
    component: PessoasComponent,
    data: { funcionario: true },
    canActivate: [acessoEquipe],
  },
  { path: 'categorias', component: CategoriasComponent, canActivate: [acessoEquipe] },
  { path: 'itens', component: ItensComponent, canActivate: [acessoEquipe] },
  { path: 'entradas', component: EntradasComponent, canActivate: [acessoEquipe] },
  { path: 'estoque', component: EstoqueComponent, canActivate: [acessoEquipe] },
  { path: 'atendimentos', component: AtendimentosComponent, canActivate: [acessoEquipe] },
  { path: '**', redirectTo: 'loja' },
];
