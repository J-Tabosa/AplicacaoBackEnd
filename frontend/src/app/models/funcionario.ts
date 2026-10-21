import { Endereco } from './endereco';

export class Funcionario {
  idpessoa = 0;
  nome = '';
  cpf = 0;
  dataNascimento = '';
  sexo: string | null = 'N';
  email: string | null = null;
  telefone: number | null = null;
  ativoPessoa: string | null = 'S';
  enderecos: Endereco[] = [new Endereco()];
  cargo = '';
  dataAdmissao = '';
}
