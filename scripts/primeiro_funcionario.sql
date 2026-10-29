-- Somente se o banco ainda não possui funcionários.
-- Troque os dados fictícios abaixo pelos dados do responsável pela loja.
-- CPF + nascimento identificam a pessoa; não existe senha nesta atividade.
START TRANSACTION;
INSERT INTO pessoa(nome, cpf, data_nascimento, sexo, email, telefone, ativo_pessoa)
VALUES ('Responsavel da loja', 90000000001, '1990-01-01', 'N', NULL, NULL, 'S');
SET @id = LAST_INSERT_ID();
INSERT INTO endereco(idpessoa, cep, numero, complemento)
VALUES (@id, '49000000', 'S/N', 'Loja');
INSERT INTO funcionario(idpessoa, cargo, data_admissao)
VALUES (@id, 'Responsavel', CURRENT_DATE());
COMMIT;
