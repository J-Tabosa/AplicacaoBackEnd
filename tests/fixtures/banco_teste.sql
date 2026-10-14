CREATE DATABASE IF NOT EXISTS atividade_backend_test;
USE atividade_backend_test;

/* =========================================================
   PESSOA
   ========================================================= */
CREATE TABLE pessoa (
    idpessoa INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    cpf BIGINT NOT NULL UNIQUE,
    data_nascimento DATE NOT NULL,
    sexo CHAR(1),
    email VARCHAR(100),
    telefone BIGINT,
    ativo_pessoa CHAR(1) DEFAULT 'S'
);

/* =========================================================
   ENDEREÇO
   ========================================================= */
CREATE TABLE endereco (
    idendereco INT AUTO_INCREMENT PRIMARY KEY,
    idpessoa INT NOT NULL,
    cep CHAR(8) NOT NULL,
    numero CHAR(10),
    complemento VARCHAR(150),

    CONSTRAINT fk_endereco_pessoa
        FOREIGN KEY (idpessoa)
        REFERENCES pessoa(idpessoa)
);

/* =========================================================
   FUNCIONÁRIO
   ========================================================= */
CREATE TABLE funcionario (
    idpessoa INT PRIMARY KEY,
    cargo VARCHAR(20) NOT NULL,
    data_admissao DATE NOT NULL,

    CONSTRAINT fk_funcionario_pessoa
        FOREIGN KEY (idpessoa)
        REFERENCES pessoa(idpessoa)
);

/* =========================================================
   CATEGORIA
   ========================================================= */
CREATE TABLE categoria (
    idcategoria INT AUTO_INCREMENT PRIMARY KEY,
    categoria VARCHAR(35) NOT NULL
);

/* =========================================================
   ITEM
   ========================================================= */
CREATE TABLE item (
    iditem INT AUTO_INCREMENT PRIMARY KEY,
    idcategoria INT NOT NULL,
    item VARCHAR(40) NOT NULL,
    descricao_item VARCHAR(200),
    preco DECIMAL(10,2) NOT NULL,
    tipo_item CHAR(1) NOT NULL,
    ativo_produto_servico CHAR(1) DEFAULT 'S',

    CONSTRAINT fk_item_categoria
        FOREIGN KEY (idcategoria)
        REFERENCES categoria(idcategoria)
);

/* =========================================================
   PRODUTO
   ========================================================= */
CREATE TABLE produto (
    iditem INT PRIMARY KEY,
    codigo_preco INT,
    preco_venda DECIMAL(10,2),
    unidade CHAR(10),

    CONSTRAINT fk_produto_item
        FOREIGN KEY (iditem)
        REFERENCES item(iditem)
);

/* =========================================================
   ESTOQUE
   ========================================================= */
CREATE TABLE estoque (
    iditem INT PRIMARY KEY,
    quantidade_estoque INT NOT NULL DEFAULT 0,

    CONSTRAINT fk_estoque_produto
        FOREIGN KEY (iditem)
        REFERENCES produto(iditem)
);

/* =========================================================
   ENTRADA DE PRODUTO
   ========================================================= */
CREATE TABLE entrada_produto (
    identrada_produto INT AUTO_INCREMENT PRIMARY KEY,
    iditem INT NOT NULL,
    data_entrada_produto DATE NOT NULL,
    quantidade_entrada DECIMAL(10,2) NOT NULL,
    unidade CHAR(10),
    valor_unitario_entrada DECIMAL(10,2) NOT NULL,

    CONSTRAINT fk_entrada_produto_item
        FOREIGN KEY (iditem)
        REFERENCES produto(iditem)
);

/* =========================================================
   ATENDIMENTO
   ========================================================= */
CREATE TABLE atendimento (
    idatendimento INT AUTO_INCREMENT PRIMARY KEY,
    idpessoa INT NOT NULL,
    data_atendimento DATE NOT NULL,
    status_atendimento CHAR(1),

    CONSTRAINT fk_atendimento_pessoa
        FOREIGN KEY (idpessoa)
        REFERENCES pessoa(idpessoa)
);

/* =========================================================
   ITENS DO ATENDIMENTO
   ========================================================= */
CREATE TABLE atendimentoitem (
    idatendimento INT NOT NULL,
    iditem INT NOT NULL,
    idfuncionario INT NOT NULL,
    quantidade_item INT NOT NULL,
    valorunitario_entrada DECIMAL(10,2),

    PRIMARY KEY (idatendimento, iditem),

    CONSTRAINT fk_atendimentoitem_atendimento
        FOREIGN KEY (idatendimento)
        REFERENCES atendimento(idatendimento),

    CONSTRAINT fk_atendimentoitem_item
        FOREIGN KEY (iditem)
        REFERENCES item(iditem),

    CONSTRAINT fk_atendimentoitem_funcionario
        FOREIGN KEY (idfuncionario)
        REFERENCES funcionario(idpessoa)
);
/* ================FUNÇÃO CALCULANDO PREÇO DE VENDA ============================ */
DELIMITER $$

CREATE FUNCTION fn_calcular_preco_venda(
    p_iditem INT
)
RETURNS DECIMAL(10,2)
DETERMINISTIC
READS SQL DATA
BEGIN
    DECLARE v_preco DECIMAL(10,2);
    DECLARE v_codigo_preco INT;
    DECLARE v_preco_venda DECIMAL(10,2);

    SELECT i.preco, p.codigo_preco
    INTO v_preco, v_codigo_preco
    FROM item i
    INNER JOIN produto p
        ON i.iditem = p.iditem
    WHERE i.iditem = p_iditem;

    IF v_codigo_preco = 1 THEN
        SET v_preco_venda = v_preco * 1.04;

    ELSEIF v_codigo_preco = 2 THEN
        SET v_preco_venda = v_preco * 1.08;

    ELSEIF v_codigo_preco = 3 THEN
        SET v_preco_venda = v_preco * 1.10;

    ELSE
        SET v_preco_venda = v_preco;
    END IF;

    RETURN v_preco_venda;
END $$

DELIMITER ;

/* Testando... */



/* ================TRIGGER CRIANDO ESTOQUE DO PRODUTO ============================ */
DELIMITER $$

CREATE TRIGGER trg_produto_estoque
AFTER INSERT ON produto
FOR EACH ROW
BEGIN
    INSERT INTO estoque (iditem, quantidade_estoque)
    VALUES (NEW.iditem, 0);
END $$

DELIMITER ;

/* ================TRIGGER ATUALIZANDO ENTRADA DO PRODUTO ============================ */
DELIMITER $$

CREATE TRIGGER trg_entrada_produto
AFTER INSERT ON entrada_produto
FOR EACH ROW
BEGIN
    UPDATE item
    SET preco = NEW.valor_unitario_entrada
    WHERE iditem = NEW.iditem;

    UPDATE produto
    SET preco_venda = fn_calcular_preco_venda(NEW.iditem)
    WHERE iditem = NEW.iditem;

    UPDATE estoque
    SET quantidade_estoque = quantidade_estoque + NEW.quantidade_entrada
    WHERE iditem = NEW.iditem;
END $$

DELIMITER ;



/* ================TRIGGER DEBITANDO ESTOQUE ============================ */
DELIMITER $$

CREATE TRIGGER trg_atendimentoitem_estoque
BEFORE INSERT ON atendimentoitem
FOR EACH ROW
BEGIN
    DECLARE v_estoque DECIMAL(10,2);
    DECLARE v_produto INT DEFAULT 0;

    SELECT COUNT(*)
    INTO v_produto
    FROM produto
    WHERE iditem = NEW.iditem;

    IF v_produto > 0 THEN

        SELECT quantidade_estoque
        INTO v_estoque
        FROM estoque
        WHERE iditem = NEW.iditem;

        IF v_estoque < NEW.quantidade_item THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Estoque insuficiente para realizar o atendimento';
        ELSE
            UPDATE estoque
            SET quantidade_estoque = quantidade_estoque - NEW.quantidade_item
            WHERE iditem = NEW.iditem;
        END IF;

    END IF;
END $$

DELIMITER ;
/* ================================================================= */