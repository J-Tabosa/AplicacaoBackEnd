"""Somente contra banco isolado; requer migração senha_imagem_produto.sql.
Dados e senhas fictícios. Verifica autenticação, manutenção da senha e BLOB por HTTP.
"""
import argparse
import base64
import http.cookiejar
import json
import os
import urllib.error
import urllib.request
import uuid

parser = argparse.ArgumentParser()
parser.add_argument('--base-url', default='http://127.0.0.1:5089')
base = parser.parse_args().base_url.rstrip('/') + '/api/'
contador = 0
SENHA = 'Teste!12345'
NOVA = 'Outra!56789'
PNG = base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAFAAAABQCAIAAAABc2X6AAAAeElEQVR4nO3PAQ3AIADAMEAhSlCHwLv4k71VsM1z9/iT9XXA2wzXGa4zXGe4znCd4TrDdYbrDNcZrjNcZ7jOcJ3hOsN1husM1xmuM1xnuM5wneE6w3WG6wzXGa4zXGe4znCd4TrDdYbrDNcZrjNcZ7jOcJ3hOsN1D7LmAhILoA32AAAAAElFTkSuQmCC')

class Cliente:
    def __init__(self):
        self.cookies = http.cookiejar.CookieJar()
        self.http = urllib.request.build_opener(urllib.request.ProxyHandler({}), urllib.request.HTTPCookieProcessor(self.cookies))
    def call(self, method, route, body=None, expected=200, arquivo=None):
        global contador
        headers = {'Content-Type': 'application/json'}
        data = json.dumps(body).encode() if body is not None else None
        if arquivo:
            nome, mime, conteudo = arquivo
            boundary = 'teste' + uuid.uuid4().hex
            headers['Content-Type'] = 'multipart/form-data; boundary=' + boundary
            data = (f'--{boundary}\r\nContent-Disposition: form-data; name="arquivo"; filename="{nome}"\r\nContent-Type: {mime}\r\n\r\n'.encode() + conteudo + f'\r\n--{boundary}--\r\n'.encode())
        request = urllib.request.Request(base + route, method=method, data=data, headers=headers)
        try:
            with self.http.open(request, timeout=30) as r: status, raw, tipo = r.status, r.read(), r.headers.get('Content-Type','')
        except urllib.error.HTTPError as r: status, raw, tipo = r.code, r.read(), r.headers.get('Content-Type','')
        contador += 1
        resposta = json.loads(raw) if raw and 'json' in tipo else raw if raw else None
        assert status in (expected if isinstance(expected, tuple) else (expected,)), (method,route,status,expected,resposta)
        return resposta

admin, cliente, outro, anonimo = Cliente(), Cliente(), Cliente(), Cliente()
admin.call('POST','Sessao', {'login':os.environ.get('API_TEST_ADMIN_LOGIN','90000000001'),'senha':os.environ.get('API_TEST_ADMIN_SENHA',SENHA)})
anonimo.call('POST','Sessao',{'cpf':90000000001,'dataNascimento':'1990-01-01'},400)
for login,senha in [('90000000001','errada'),('nao-existe@example.com',SENHA),('90000000002',SENHA)]:
    anonimo.call('POST','Sessao',{'login':login,'senha':senha},401)

def pessoa(cpf,email):
    return {'nome':'Senha imagem teste','cpf':cpf,'dataNascimento':'2000-01-15','sexo':'N','email':email,
            'ativoPessoa':'S','senha':SENHA,'enderecos':[{'cep':'49000000','numero':'10'}]}

dados=pessoa(76000000001,'senha.imagem@example.com')
sem_senha=dict(dados);del sem_senha['senha']
anonimo.call('POST','PessoaEndereco',sem_senha,400)
anonimo.call('POST','PessoaEndereco',dict(dados,senha='curta'),400)
pa=anonimo.call('POST','PessoaEndereco',dados,201)
pb=anonimo.call('POST','PessoaEndereco',pessoa(76000000002,'senha.outro@example.com'),201)
assert 'senha' not in pa and 'senhaHash' not in pa and SENHA not in json.dumps(pa)
assert not cliente.call('POST','Sessao',{'login':'  SENHA.IMAGEM@EXAMPLE.COM  ','senha':SENHA})['funcionario']
assert cliente.call('GET','Sessao')['idpessoa']==pa['idpessoa']
cliente.call('POST','Sessao',{'login':'760.000.000-01','senha':SENHA})
assert 'senhaHash' not in admin.call('GET',f"PessoaEndereco/{pa['idpessoa']}")
# PUT sem senha preserva o hash e a sessão.
sem_senha['enderecos']=pa['enderecos']
admin.call('PUT',f"PessoaEndereco/{pa['idpessoa']}",sem_senha,204)
cliente.call('GET','Sessao')
# Troca invalida cookie antigo e impede a senha anterior.
admin.call('PUT',f"PessoaEndereco/{pa['idpessoa']}",dict(sem_senha,senha=NOVA),204)
cliente.call('GET','Sessao',expected=401)
cliente.call('POST','Sessao',{'login':str(pa['cpf']),'senha':SENHA},401)
cliente.call('POST','Sessao',{'login':str(pa['cpf']),'senha':NOVA})
cliente.call('GET','PessoaEndereco/all',expected=403)

func_dados=dict(pessoa(76000000003,'senha.funcionario@example.com'),cargo='Atendente',dataAdmissao='2026-01-01')
func=admin.call('POST','Funcionario',func_dados,201)
assert 'senha' not in func and 'senhaHash' not in func and SENHA not in json.dumps(func)
assert outro.call('POST','Sessao',{'login':func_dados['email'],'senha':SENHA})['funcionario']
outro.call('GET','PessoaEndereco/all')
func_dados['enderecos']=func['enderecos']
admin.call('PUT',f"Funcionario/{func['idpessoa']}",dict(func_dados,senha=NOVA),204)
outro.call('GET','Sessao',expected=401)
outro.call('POST','Sessao',{'login':func_dados['email'],'senha':NOVA})

cat=admin.call('POST','Categoriums',{'categoria':'Imagens de teste'},201)['idcategoria']
produto_dados={'idCategoria':cat,'item':'Produto com imagem','preco':20,'tipoItem':'P','produto':{'codigoPreco':0,'precoVenda':20,'unidade':'UN'}}
produto=admin.call('POST','Items',produto_dados,201)['idItem']
servico=admin.call('POST','Items',{'idCategoria':cat,'item':'Serviço sem foto','preco':50,'tipoItem':'S'},201)['idItem']
rota=f'Items/{produto}/imagem'
anonimo.call('GET',rota,expected=404)
anonimo.call('PUT',rota,arquivo=('foto.png','image/png',PNG),expected=401)
cliente.call('PUT',rota,arquivo=('foto.png','image/png',PNG),expected=403)
cliente.call('DELETE',rota,expected=403)
admin.call('PUT',rota,arquivo=('falso.png','image/png',b'<html>nao e imagem</html>'),expected=400)
admin.call('PUT',rota,arquivo=('foto.svg','image/svg+xml',b'<svg/>'),expected=400)
admin.call('PUT',rota,arquivo=('foto.jpg','image/jpeg',PNG),expected=400)
admin.call('PUT',rota,arquivo=('foto.txt','image/png',PNG),expected=400)
admin.call('PUT',rota,arquivo=('vazia.png','image/png',b''),expected=400)
admin.call('PUT',rota,arquivo=('grande.png','image/png',PNG+b'x'*(3*1024*1024)),expected=(400,413))
admin.call('PUT',f'Items/{servico}/imagem',arquivo=('foto.png','image/png',PNG),expected=404)
admin.call('PUT','Items/99999999/imagem',arquivo=('foto.png','image/png',PNG),expected=404)
for extensao, mime, codificada in [('jpg', 'image/jpeg', '/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAAMAAwDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDRooor548M/9k='), ('webp', 'image/webp', 'UklGRjgAAABXRUJQVlA4ICwAAADQAQCdASoMAAwAAUAmJaACdLoB+AADsAD+4ka/8gvVvm27f/FnIxHZeAAAAA==')]:
    conteudo = base64.b64decode(codificada)
    admin.call('PUT',rota,arquivo=('foto.'+extensao,mime,conteudo))
    assert anonimo.call('GET',rota)==conteudo
imagem=admin.call('PUT',rota,arquivo=('foto.png','image/png',PNG))
assert anonimo.call('GET',rota)==PNG
item=anonimo.call('GET',f'Items/{produto}')
assert item['imagemUrl']==imagem['imagemUrl']
assert 'dados' not in item and 'base64' not in json.dumps(item).lower()
assert any(i['idItem']==produto and i['imagemUrl']==imagem['imagemUrl'] for i in anonimo.call('GET','Items'))
assert admin.call('GET',f'ProdutoItem/{produto}')['imagemUrl']==imagem['imagemUrl']
nova=admin.call('PUT',rota,arquivo=('substituida.png','image/png',PNG))
assert nova['imagemUrl'] != imagem['imagemUrl']
admin.call('PUT',f'Items/{produto}',dict(produto_dados,ativoProdutoServico='N'),204)
anonimo.call('GET',rota,expected=404)
assert admin.call('GET',rota)==PNG
admin.call('PUT',f'Items/{produto}',dict(produto_dados,ativoProdutoServico='S'),204)
admin.call('DELETE',rota,expected=204)
admin.call('DELETE',rota,expected=204)
assert anonimo.call('GET',f'Items/{produto}')['imagemUrl'] is None
admin.call('PUT',rota,arquivo=('foto.png','image/png',PNG))
admin.call('DELETE',f'Items/{produto}',expected=204)
anonimo.call('GET',rota,expected=404)
cliente.call('DELETE','Sessao',expected=204)
cliente.call('GET','Sessao',expected=401)
print(f'PASSOU SENHA/IMAGEM: {contador} requisições; login, hash oculto, troca de senha, sessão e upload.',flush=True)
