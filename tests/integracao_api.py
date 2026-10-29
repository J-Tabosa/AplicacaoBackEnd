"""Teste de integração: executar contra uma API ligada a um banco de teste vazio.

Cria cadastros fictícios e verifica transações, relacionamentos e saldos reais.
Usa apenas a biblioteca padrão do Python. Veja o README antes de executar.
"""
import argparse
import http.cookiejar
import os
import concurrent.futures
import json
import time
import urllib.error
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('--base-url', default='http://127.0.0.1:5088')
args = parser.parse_args()
BASE = args.base_url.rstrip('/')
opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
requests_count = 0


def call(method, route, body=None, expected=200):
    global requests_count
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(BASE + '/api/' + route, data=data, method=method,
                                 headers={'Content-Type': 'application/json'})
    try:
        with opener.open(req, timeout=30) as response:
            status, payload = response.status, response.read()
    except urllib.error.HTTPError as error:
        status, payload = error.code, error.read()
    requests_count += 1
    result = json.loads(payload) if payload else None
    if expected is not None:
        assert status == expected, (method, route, status, expected, result)
    return (status, result) if expected is None else result


def stock(item_id, expected):
    item = call('GET', f'Items/{item_id}')
    assert item['produto']['quantidadeEstoque'] == expected, item


seed = int(time.time()) % 1000000000


def person(cpf, name):
    return {
        'nome': name, 'cpf': cpf, 'dataNascimento': '2000-01-15', 'sexo': 'M',
        'email': f'{cpf}@example.com', 'telefone': 79999999999, 'ativoPessoa': 'S',
        'enderecos': [{'cep': '49000000', 'numero': '10', 'complemento': 'Teste A'},
                      {'cep': '49000111', 'numero': '20', 'complemento': 'Teste B'}]
    }


call('POST', 'Sessao', {'cpf': int(os.environ.get('API_TEST_ADMIN_CPF', '90000000001')), 'dataNascimento': os.environ.get('API_TEST_ADMIN_NASCIMENTO', '1990-01-01')})

categoria = call('POST', 'Categoriums', {'categoria': f'Integracao {seed}'}, 201)['idcategoria']
cliente_data = person(10000000000 + seed, 'Cliente de integração')
cliente = call('POST', 'PessoaEndereco', cliente_data, 201)
cliente_id = cliente['idpessoa']
assert len(cliente['enderecos']) == 2
assert any(p['idpessoa'] == cliente_id for p in call('GET', 'PessoaEndereco/all'))

func_data = person(20000000000 + seed, 'Funcionário de integração')
func_data.update(cargo='Atendente', dataAdmissao='2026-10-14')
func = call('POST', 'Funcionario', func_data, 201)
func_id = func['idpessoa']
assert len(func['enderecos']) == 2
func_data['nome'] = 'Nome atualizado'
func_data['cargo'] = 'Supervisor'
func_data['enderecos'] = [dict(func['enderecos'][0]), {'cep': '49000222', 'numero': '30'}]
call('PUT', f'Funcionario/{func_id}', func_data, 204)
func_atual = call('GET', f'Funcionario/{func_id}')
assert func_atual['nome'] == 'Nome atualizado' and func_atual['cargo'] == 'Supervisor'
assert len(func_atual['enderecos']) == 2
assert func['enderecos'][1]['idendereco'] not in [e['idendereco'] for e in func_atual['enderecos']]
call('POST', 'Funcionario', dict(func_data, cpf=30000000000 + seed), 400)  # Cadastro novo não aceita ID de endereço existente.
duplicado = person(func_data['cpf'], 'CPF duplicado')
duplicado.update(cargo='Atendente', dataAdmissao='2026-10-14')
call('POST', 'Funcionario', duplicado, 409)
assert any(f['idpessoa'] == func_id for f in call('GET', 'Funcionario/all'))
print('OK: pessoa, funcionário completo, endereços e CPF duplicado', flush=True)

produto = {'idCategoria': categoria, 'item': 'Produto A', 'descricaoItem': 'Teste', 'preco': 10,
           'tipoItem': 'P', 'ativoProdutoServico': 'S', 'codigoPreco': 1, 'precoVenda': 10.4, 'unidade': 'UN'}
a = call('POST', 'ProdutoItem', produto, 201)['idItem']
b_data = {'idCategoria': categoria, 'item': 'Produto B', 'preco': 20, 'tipoItem': 'P',
          'produto': {'codigoPreco': 3, 'precoVenda': 22, 'unidade': 'UN'}}
b = call('POST', 'Items', b_data, 201)['idItem']
c = call('POST', 'ProdutoItem', dict(produto, item='Produto C'), 201)['idItem']
servico_data = {'idCategoria': categoria, 'item': 'Serviço', 'preco': 50, 'tipoItem': 'S'}
s = call('POST', 'Items', servico_data, 201)['idItem']
stock(a, 0)
stock(b, 0)
call('POST', 'Items', {'idCategoria': categoria, 'item': 'P inválido', 'preco': 1, 'tipoItem': 'P'}, 400)
servico_data['item'] = 'Serviço atualizado'
call('PUT', f'Items/{s}', servico_data, 204)
assert call('GET', f'Items/{s}')['item'] == 'Serviço atualizado'
print('OK: produtos, serviços e DTOs', flush=True)


def entry(item, quantity, value=10):
    return {'idItem': item, 'dataEntradaProduto': '2026-10-14', 'quantidadeEntrada': quantity,
            'unidade': 'UN', 'valorUnitarioEntrada': value}


e1 = call('POST', 'EntradaProduto', entry(a, 10, 20), 201)['idEntradaProduto']
stock(a, 10)
assert call('GET', f'ProdutoItem/{a}')['precoVenda'] == 20.8
call('PUT', f'EntradaProduto/{e1}', entry(a, 15, 21), 204)
stock(a, 15)
assert call('GET', f'ProdutoItem/{a}')['precoVenda'] == 21.84
call('PUT', f'EntradaProduto/{e1}', entry(b, 6, 30), 204)
stock(a, 0)
stock(b, 6)
assert call('GET', f'ProdutoItem/{b}')['precoVenda'] == 33
call('POST', 'EntradaProduto', entry(a, -1), 400)
call('POST', 'EntradaProduto', entry(a, 0.5), 400)
call('POST', 'EntradaProduto', entry(999999999, 1), 404)
e2 = call('POST', 'EntradaProduto', entry(a, 10), 201)['idEntradaProduto']
stock(a, 10)
assert len(call('GET', 'EntradaProduto/all')) == 2
print('OK: entradas, transferência entre produtos, saldo e preço', flush=True)


def ai(item, quantity, price=12, employee=func_id):
    return {'idItem': item, 'idFuncionario': employee, 'quantidadeItem': quantity, 'valorUnitarioEntrada': price}


def attendance(items):
    return {'idPessoa': cliente_id, 'dataAtendimento': '2026-10-14', 'statusAtendimento': 'A', 'itens': items}


at = call('POST', 'Atendimento', attendance([ai(a, 3), ai(s, 1, 50)]), 201)
at_id = at['idAtendimento']
assert at['cliente'] == cliente_data['nome'] and len(at['itens']) == 2 and at['valorTotal'] == 86
assert at['pessoa']['cpf'] == cliente_data['cpf'] and len(at['pessoa']['enderecos']) == 2
assert all(i['funcionario'] == 'Nome atualizado' for i in at['itens'])
stock(a, 7)
call('DELETE', f'Funcionario/{func_id}', expected=409)
call('DELETE', f'PessoaEndereco/{cliente_id}', expected=409)
count_before = len(call('GET', 'Atendimento/all'))
call('POST', 'Atendimento', attendance([ai(a, 1000)]), 409)
call('POST', 'Atendimento', attendance([ai(a, 1, employee=999999999)]), 404)
call('POST', 'Atendimento', attendance([ai(a, 1), ai(a, 1)]), 400)
assert len(call('GET', 'Atendimento/all')) == count_before
stock(a, 7)

updated = attendance([ai(a, 5), ai(s, 2, 40)])
call('PUT', f'Atendimento/{at_id}', updated, 204)
stock(a, 5)
call('PUT', f'Atendimento/{at_id}', updated, 204)
stock(a, 5)  # PUT repetido não pode baixar o estoque novamente.
call('PUT', f'Atendimento/{at_id}', dict(updated, statusAtendimento='F', itens=[ai(a, 999)]), 409)
assert call('GET', f'Atendimento/{at_id}')['statusAtendimento'] == 'A'
stock(a, 5)

call('POST', f'Atendimento/{at_id}/itens', ai(b, 2), 201)
stock(b, 4)
call('PUT', f'Atendimento/{at_id}/itens/{b}', ai(b, 4), 204)
stock(b, 2)
call('DELETE', f'Atendimento/{at_id}/itens/{b}', expected=204)
stock(b, 6)
call('POST', f'Atendimento/{at_id}/itens', ai(a, 1), 409)
call('DELETE', f'Atendimento/{at_id}/itens/{s}', expected=204)
call('DELETE', f'Atendimento/{at_id}/itens/{a}', expected=409)
call('DELETE', f'Atendimento/{at_id}', expected=204)
stock(a, 10)
call('GET', f'Atendimento/{at_id}', expected=404)
print('OK: atendimento N:N, serviços, itens, PUT idempotente e rollback', flush=True)

consumo = call('POST', 'Atendimento', attendance([ai(a, 8)]), 201)['idAtendimento']
stock(a, 2)
call('DELETE', f'EntradaProduto/{e2}', expected=409)
call('PUT', f'EntradaProduto/{e2}', entry(a, 1), 409)
stock(a, 2)
call('GET', f'EntradaProduto/{e2}')
call('DELETE', f'Atendimento/{consumo}', expected=204)
stock(a, 10)
call('DELETE', f'EntradaProduto/{e2}', expected=204)
stock(a, 0)
print('OK: bloqueio de estorno de entrada já consumida', flush=True)

e3 = call('POST', 'EntradaProduto', entry(c, 5), 201)['idEntradaProduto']
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    futures = [pool.submit(call, 'POST', 'Atendimento', attendance([ai(c, 4)]), None) for _ in range(2)]
    results = [f.result() for f in futures]
assert sorted(status for status, _ in results) == [201, 409], results
stock(c, 1)
for status, result in results:
    if status == 201:
        call('DELETE', f"Atendimento/{result['idAtendimento']}", expected=204)
stock(c, 5)
print('OK: duas vendas simultâneas não produzem estoque negativo', flush=True)

# Rotas equivalentes devem continuar usando os mesmos dados e validações.
call('PUT', f'Categoriums/{categoria}', {'categoria': 'Categoria atualizada'}, 204)
assert call('GET', f'Categoriums/{categoria}')['categoria'] == 'Categoria atualizada'
call('DELETE', f'Categoriums/{categoria}', expected=409)

produto_atualizado = dict(produto, item='Produto A atualizado', precoVenda=15.90)
call('PUT', f'ProdutoItem/{a}', produto_atualizado, 204)
assert call('GET', f'ProdutoItem/{a}')['precoVenda'] == 15.90
b_data['produto']['precoVenda'] = 35.90
call('PUT', f'Items/{b}', b_data, 204)
assert call('GET', f'Items/{b}')['produto']['precoVenda'] == 35.90
call('GET', f'ProdutoItem/{s}', expected=404)
assert any(p['idItem'] == a for p in call('GET', 'ProdutoItem'))
call('PUT', f'Items/{s}', dict(servico_data, tipoItem='P', produto=b_data['produto']), 409)

cliente_atual = call('GET', f'PessoaEndereco/{cliente_id}')
cliente_data['nome'] = 'Cliente atualizado'
cliente_data['enderecos'] = [dict(cliente_atual['enderecos'][0])]
call('PUT', f'PessoaEndereco/{cliente_id}', cliente_data, 204)
cliente_atual = call('GET', f'PessoaEndereco/{cliente_id}')
assert cliente_atual['nome'] == 'Cliente atualizado' and len(cliente_atual['enderecos']) == 1

# Respostas inválidas não podem alterar um cadastro já existente.
call('PUT', f'PessoaEndereco/{cliente_id}', dict(cliente_data, enderecos=[]), 400)
call('PUT', f'Funcionario/{func_id}', dict(func_data, dataAdmissao=None), 400)
call('PUT', f'Funcionario/{func_id}', dict(func_data, enderecos=[dict(cliente_atual['enderecos'][0])]), 400)
assert call('GET', f'Funcionario/{func_id}')['cargo'] == 'Supervisor'
call('POST', 'ProdutoItem', dict(produto, precoVenda=-1), 400)
call('POST', 'Items', dict(servico_data, produto=b_data['produto']), 400)
call('POST', 'EntradaProduto', dict(entry(a, 1), dataEntradaProduto=None), 400)
print('OK: categorias, PUT dos dois cadastros de produto, pessoa e validações dos DTOs', flush=True)

call('DELETE', f'EntradaProduto/{e1}', expected=204)
call('DELETE', f'EntradaProduto/{e3}', expected=204)
for item in (a, b, c):
    call('DELETE', f'ProdutoItem/{item}', expected=204)
call('DELETE', f'Items/{s}', expected=204)
call('DELETE', f'Categoriums/{categoria}', expected=204)
call('DELETE', f'Funcionario/{func_id}', expected=204)
call('GET', f'PessoaEndereco/{func_id}')  # Excluir o vínculo não apaga a pessoa.
call('DELETE', f'PessoaEndereco/{func_id}', expected=204)
call('DELETE', f'PessoaEndereco/{cliente_id}', expected=204)
print(f'PASSOU: {requests_count} requisições HTTP verificadas.', flush=True)
