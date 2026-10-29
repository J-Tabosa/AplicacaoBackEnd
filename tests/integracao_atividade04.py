"""Atividade 04: executar apenas contra banco de teste isolado.

Requer fixtures/administrador_teste.sql. Pedidos concluídos são preservados
pela API; recrie o banco de teste ao repetir a suíte.
"""
import argparse
import concurrent.futures
import http.cookiejar
import json
import os
import time
import urllib.error
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('--base-url', default='http://127.0.0.1:5089')
args = parser.parse_args()
base = args.base_url.rstrip('/') + '/api/'
count = 0


class Cliente:
    def __init__(self):
        self.cookies = http.cookiejar.CookieJar()
        self.http = urllib.request.build_opener(urllib.request.ProxyHandler({}),
            urllib.request.HTTPCookieProcessor(self.cookies))

    def call(self, method, route, body=None, expected=200):
        global count
        request = urllib.request.Request(base + route,
            data=json.dumps(body).encode() if body is not None else None,
            method=method, headers={'Content-Type': 'application/json'})
        try:
            with self.http.open(request, timeout=30) as response:
                status, raw = response.status, response.read()
        except urllib.error.HTTPError as response:
            status, raw = response.code, response.read()
        count += 1
        data = json.loads(raw) if raw else None
        if expected is None:
            return status, data
        assert status == expected, (method, route, status, expected, data)
        return data


seed = int(time.time()) % 10000000
admin, anonimo, a, b = Cliente(), Cliente(), Cliente(), Cliente()
sessao = admin.call('POST', 'Sessao', {
    'cpf': int(os.environ.get('API_TEST_ADMIN_CPF', '90000000001')),
    'dataNascimento': os.environ.get('API_TEST_ADMIN_NASCIMENTO', '1990-01-01')})
assert sessao['funcionario']
for route in ['Atendimento/all', 'Estoque', 'PessoaEndereco/all', 'Atendimento/meus']:
    anonimo.call('GET', route, expected=401)
anonimo.call('POST', 'Sessao', {'cpf': 1, 'dataNascimento': '2000-01-01'}, 401)


def pessoa(cpf, nome, email):
    return {'nome': nome, 'cpf': cpf, 'dataNascimento': '2000-01-15',
            'email': email, 'ativoPessoa': 'S',
            'enderecos': [{'cep': '01001000', 'numero': '10'}]}


cpf_a, cpf_b = 71000000000 + seed, 72000000000 + seed
pa = a.call('POST', 'PessoaEndereco', pessoa(cpf_a, 'Cliente A atividade04', f'a{seed}@example.com'), 201)
pb = b.call('POST', 'PessoaEndereco', pessoa(cpf_b, 'Cliente B atividade04', f'b{seed}@example.com'), 201)
anonimo.call('POST', 'PessoaEndereco', pessoa(73000000000 + seed, 'Email duplicado', f'A{seed}@EXAMPLE.COM'), 409)
assert not a.call('POST', 'Sessao', {'cpf': cpf_a, 'dataNascimento': '2000-01-15'})['funcionario']
b.call('POST', 'Sessao', {'cpf': cpf_b, 'dataNascimento': '2000-01-15'})
a.call('GET', 'Atendimento/all', expected=403)
a.call('GET', f"PessoaEndereco/{pb['idpessoa']}", expected=403)
a.call('POST', 'Funcionario', dict(pessoa(74000000000 + seed, 'Promocao indevida', None), cargo='Admin', dataAdmissao='2026-01-01'), 403)
assert a.call('GET', 'Atendimento/meus') == []

cat = admin.call('POST', 'Categoriums', {'categoria': f'Loja04 {seed}'}, 201)['idcategoria']
produto_data = {'idCategoria': cat, 'item': f'Creme04 {seed}', 'tipoItem': 'P', 'preco': 10,
    'produto': {'codigoPreco': 1, 'precoVenda': 10.4, 'unidade': 'UN'}}
p = admin.call('POST', 'Items', produto_data, 201)['idItem']
servico_data = {'idCategoria': cat, 'item': f'Limpeza04 {seed}', 'tipoItem': 'S', 'preco': 50}
s = admin.call('POST', 'Items', servico_data, 201)['idItem']
inativo_data = dict(servico_data, item=f'Inativo04 {seed}', ativoProdutoServico='N')
n = admin.call('POST', 'Items', inativo_data, 201)['idItem']
admin.call('POST', 'EntradaProduto', {'idItem': p, 'dataEntradaProduto': '2026-10-29',
    'quantidadeEntrada': 5, 'unidade': 'UN', 'valorUnitarioEntrada': 12}, 201)
assert admin.call('GET', f'Estoque/{p}')['quantidadeEstoque'] == 5
assert any(i['idItem'] == p for i in admin.call('GET', 'Estoque'))
admin.call('GET', 'Estoque/99999999', expected=404)
lista = anonimo.call('GET', f'Items?nome=04%20{seed}&idCategoria={cat}&tipo=P')
assert [i['idItem'] for i in lista] == [p]
assert [i['idItem'] for i in anonimo.call('GET', f'Items?idCategoria={cat}&tipo=S')] == [s]
anonimo.call('GET', f'Items/{n}', expected=404)
assert all(i['idItem'] != n for i in anonimo.call('GET', f'Items?idCategoria={cat}&ativo=N'))
anonimo.call('GET', 'Items/99999999', expected=404)


def compra(*linhas):
    return {'itens': [{'idItem': item, 'quantidadeItem': qtd} for item, qtd in linhas]}


a.call('POST', 'Atendimento/checkout', compra((p, 0)), 400)
a.call('POST', 'Atendimento/checkout', compra((p, 1), (p, 1)), 400)
a.call('POST', 'Atendimento/checkout', compra((n, 1)), 409)
a.call('POST', 'Atendimento/checkout', compra((p, 100)), 409)
assert a.call('GET', 'Atendimento/meus') == []
assert admin.call('GET', f'Estoque/{p}')['quantidadeEstoque'] == 5
payload = compra((p, 2), (s, 1))
payload['idPessoa'] = pb['idpessoa']
payload['itens'][0].update(valorUnitarioEntrada=0.01, idFuncionario=99999999)
pedido = a.call('POST', 'Atendimento/checkout', payload, 201)
id_pedido = pedido['idAtendimento']
assert pedido['idPessoa'] == pa['idpessoa']
assert pedido['statusAtendimento'] == 'A'
assert pedido['valorTotal'] == 74.96
assert all(i['cargoFuncionario'] for i in pedido['itens'])
assert pedido['pessoa']['enderecos'][0]['cep'] == '01001000'
assert admin.call('GET', f'Estoque/{p}')['quantidadeEstoque'] == 3
assert len(a.call('GET', 'Atendimento/meus')) == 1
b.call('GET', f'Atendimento/meus/{id_pedido}', expected=404)
assert b.call('GET', f"Atendimento/meus?idPessoa={pa['idpessoa']}") == []
b.call('GET', f'Atendimento/{id_pedido}', expected=403)
b.call('PUT', f'Atendimento/{id_pedido}/status', {'statusAtendimento': 'C'}, 403)
assert any(x['idAtendimento'] == id_pedido for x in admin.call('GET', 'Atendimento/all?status=Aberto'))
admin.call('GET', 'Atendimento/all?status=Invalido', expected=400)
admin.call('PUT', f'Atendimento/{id_pedido}/status', {'statusAtendimento': 'F'}, 409)
admin.call('PUT', f'Atendimento/{id_pedido}/status', {'statusAtendimento': 'P'}, 204)
assert all(x['idAtendimento'] != id_pedido for x in admin.call('GET', 'Atendimento/all?status=A'))
admin.call('POST', f'Atendimento/{id_pedido}/itens', {'idItem': s, 'idFuncionario': sessao['idpessoa'], 'quantidadeItem': 2, 'valorUnitarioEntrada': 50}, 409)
admin.call('PUT', f'Atendimento/{id_pedido}/status', {'statusAtendimento': 'F'}, 204)
admin.call('PUT', f'Atendimento/{id_pedido}/status', {'statusAtendimento': 'F'}, 204)
admin.call('PUT', f'Atendimento/{id_pedido}/status', {'statusAtendimento': 'C'}, 409)
admin.call('DELETE', f'Atendimento/{id_pedido}', expected=409)

cancelar = a.call('POST', 'Atendimento/checkout', compra((p, 1), (s, 1)), 201)['idAtendimento']
assert admin.call('GET', f'Estoque/{p}')['quantidadeEstoque'] == 2
admin.call('PUT', f'Atendimento/{cancelar}/status', {'statusAtendimento': 'C'}, 204)
assert admin.call('GET', f'Estoque/{p}')['quantidadeEstoque'] == 3
admin.call('PUT', f'Atendimento/{cancelar}/status', {'statusAtendimento': 'C'}, 204)
assert admin.call('GET', f'Estoque/{p}')['quantidadeEstoque'] == 3
assert len(a.call('GET', f'Atendimento/meus/{cancelar}')['itens']) == 2
admin.call('PUT', f'Atendimento/{cancelar}/status', {'statusAtendimento': 'P'}, 409)
admin.call('DELETE', f'Atendimento/{cancelar}', expected=204)
assert admin.call('GET', f'Estoque/{p}')['quantidadeEstoque'] == 3
admin.call('PUT', f'Items/{p}', dict(produto_data, produto={'codigoPreco': 1, 'precoVenda': 99, 'unidade': 'UN'}), 204)
assert a.call('GET', f'Atendimento/meus/{id_pedido}')['valorTotal'] == 74.96

with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    respostas = list(pool.map(lambda cliente: cliente.call('POST', 'Atendimento/checkout', compra((p, 2)), None), [a, b]))
assert sorted(r[0] for r in respostas) == [201, 409], respostas
assert admin.call('GET', f'Estoque/{p}')['quantidadeEstoque'] == 1
for status, registro in respostas:
    if status == 201:
        admin.call('PUT', f"Atendimento/{registro['idAtendimento']}/status", {'statusAtendimento': 'C'}, 204)
assert admin.call('GET', f'Estoque/{p}')['quantidadeEstoque'] == 3
a.call('DELETE', 'Sessao', expected=204)
a.call('GET', 'Atendimento/meus', expected=401)
print(f'PASSOU ATIVIDADE 04: {count} requisições; sessão, isolamento, filtros, preço, pedidos e estoque.', flush=True)
