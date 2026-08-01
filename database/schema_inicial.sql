PRAGMA foreign_keys = OFF;
BEGIN TRANSACTION;

-- Tabela de usuários (alas)
-- NENHUMA credencial é inserida aqui: os logins por role e as chaves de
-- convite são criados de forma idempotente em DbContext.cs (DatabaseInitializer)
-- com senhas/chaves ALEATÓRIAS exibidas uma única vez no console de inicialização.
CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username varchar NOT NULL,
    password varchar NOT NULL
);

-- Tabela principal de atas
CREATE TABLE IF NOT EXISTS atas (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    tipo TEXT NOT NULL,
    data TEXT NOT NULL,
    status TEXT DEFAULT 'pendente',
    ala_id INTEGER NOT NULL,
    FOREIGN KEY(ala_id) REFERENCES users(id)
);

-- Tabela para atas sacramentais
CREATE TABLE IF NOT EXISTS sacramental (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    ata_id INTEGER,
    presidido TEXT,
    dirigido TEXT,
    pianista TEXT,
    regente_musica TEXT,
    anuncios TEXT,
    hinos TEXT,
    hino_sacramental TEXT,
    hino_intermediario TEXT,
    oracoes TEXT,
    discursante_1 TEXT,
    discursante_2 TEXT,
    outros TEXT,
    tema_1 TEXT,
    tema_2 TEXT,
    tema_ultimo TEXT,
    obs_1 TEXT,
    obs_2 TEXT,
    obs_ultimo TEXT,
    recepcionistas TEXT,
    reconhecemos_presenca TEXT,
    desobrigacoes TEXT,
    apoios TEXT,
    confirmacoes_batismo TEXT,
    apoio_membros TEXT,
    bencao_criancas TEXT,
    testemunhos TEXT,
    ultimo_discursante TEXT,
    id_tipo INTEGER,
    tema TEXT,
    FOREIGN KEY(ata_id) REFERENCES atas(id),
    FOREIGN KEY(id_tipo) REFERENCES templates(id)
);

-- Tabela para atas de batismo
CREATE TABLE IF NOT EXISTS batismo (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    ata_id INTEGER,
    dedicado TEXT,
    presidido TEXT,
    dirigido TEXT,
    batizados TEXT,
    testemunha1 TEXT,
    testemunha2 TEXT,
    FOREIGN KEY(ata_id) REFERENCES atas(id) ON DELETE CASCADE
);

-- Tabela para estacas
CREATE TABLE IF NOT EXISTS estacas (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nome TEXT NOT NULL UNIQUE,
    presidente TEXT,
    primeiro_conselheiro TEXT,
    segundo_conselheiro TEXT
);

INSERT OR IGNORE INTO estacas (id, nome, presidente, primeiro_conselheiro, segundo_conselheiro) VALUES
(1, 'Criciúma', 'Alexandre Goulart Pacheco', 'Rafael Atanázio Duarte de Sá', 'Mateus Dal Toé');

-- Tabela para unidades (alas)
CREATE TABLE IF NOT EXISTS unidades (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    ala_id INTEGER NOT NULL UNIQUE,
    nome TEXT,
    bispo TEXT,
    primeiro_conselheiro TEXT,
    segundo_conselheiro TEXT,
    estaca_id INTEGER NOT NULL DEFAULT 1,
    horario TEXT,
    FOREIGN KEY(ala_id) REFERENCES users(id),
    FOREIGN KEY(estaca_id) REFERENCES estacas(id)
);

INSERT OR IGNORE INTO unidades (id, ala_id, nome, bispo, primeiro_conselheiro, segundo_conselheiro, estaca_id, horario) VALUES
(1, 1, 'Ala Criciúma 1', 'Julio Davila', 'Antonio Carlos de Souza', 'Ari Cesar Albeche Lopes', 1, '09:30 - 10:30'),
(2, 2, 'Ala Criciúma 2', 'alterar', 'alterar', 'alterar', 1, 'alterar'),
(3, 3, 'Ala Criciúma 3', 'alterar', 'alterar', 'alterar', 1, 'alterar'),
(4, 4, 'Ala Içara', 'alterar', 'alterar', 'alterar', 1, 'alterar'),
(5, 5, 'Ala Araranguá', 'alterar', 'alterar', 'alterar', 1, 'alterar'),
(6, 6, 'Obra Unidade', '', '', '', 1, '00:00 - 00:00');

-- Tabela para templates corrigida
CREATE TABLE IF NOT EXISTS templates (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    ala_id INTEGER NOT NULL, -- Coluna necessária para o filtro do Python
    tipo_template INTEGER NOT NULL, -- 1: Sacramental, 2: Batismo/Testemunhos
    nome TEXT NOT NULL,
    boas_vindas TEXT NOT NULL,
    desobrigacoes TEXT NOT NULL,
    apoios TEXT,
    confirmacoes_batismo TEXT NOT NULL,
    apoio_membro_novo TEXT NOT NULL,
    bencao_crianca TEXT NOT NULL,
    ordenacoes TEXT NOT NULL,
    desobrigacoes_plural TEXT NOT NULL,
    apoios_plural TEXT,
    confirmacoes_batismo_plural TEXT NOT NULL,
    apoio_membro_novo_plural TEXT NOT NULL,
    bencao_crianca_plural TEXT NOT NULL,
    ordenacoes_plural TEXT NOT NULL,
    sacramento TEXT NOT NULL,
    mensagens TEXT NOT NULL,
    live TEXT NOT NULL,
    encerramento TEXT NOT NULL,
    FOREIGN KEY (ala_id) REFERENCES users(id)
);

INSERT OR IGNORE INTO templates (ala_id, tipo_template, nome, boas_vindas, desobrigacoes, apoios, confirmacoes_batismo, apoio_membro_novo, bencao_crianca, ordenacoes, desobrigacoes_plural, apoios_plural, confirmacoes_batismo_plural, apoio_membro_novo_plural, bencao_crianca_plural, ordenacoes_plural, sacramento, mensagens, live, encerramento) 
VALUES
(
    0,
    1,
    'Sacramental Padrão',
    'Bom dia irmãos e irmãs! Gostaríamos de fazer todos muito bem vindos a mais uma Reunião Sacramental da ALA [ALA], Estaca Criciúma, neste domingo dia [DATA]. Desejamos que todos se sintam bem entre nós, especialmente aqueles que nos visitam.',
    '[NOME] está sendo desobrigado(a) como [CHAMADO]. Os que desejarem manifestar agradecimento por seus serviços prestados podem fazê-lo levantando a mão',
    '[NOME] foi chamado(a) para servir como [CHAMADO]. Os que forem a favor, manifestem-se levantando a mão. [Pequena pausa.] Os que se opuserem, se houver, manifestem-se. [Pequena pausa.]',
    'O(a) irmã(o) [NOME] foi batizado(a), gostaríamos de convidá-lo(a) para vir até o púlpito para que possamos fazer sua confirmação como membro de A Igreja de Jesus Cristo dos Santos dos Últimos Dias.',
    'O(a) irmã(o) [NOME] foi batizado e confirmado membro da igreja, e gostaríamos do apoio de todos os irmãos de plena aceitação como mais novo membro da ala. Todos a favor, manifestem-se',
    'Gostaríamos de chamar ao púlpito o irmão [NOME] que irá dar a bênção de apresentação da(o) [NOME DA CRIANÇA].',
    'É proposto que [NOME] receba o Sacerdócio de Melquisedeque e seja ordenado(a) como [CHAMADO]. Os que forem a favor, manifestem-se levantando a mão. [Pequena pausa.] Os que se opuserem, se houver, manifestem-se.',
    '[LISTA] Os que desejarem manifestar agradecimento por seus serviços prestados podem fazê-lo levantando a mão.',
    '[LISTA] Os que forem a favor, manifestem-se levantando a mão. [Pequena pausa.] Os que se opuserem, se houver, manifestem-se. [Pequena pausa.]',
    'Os irmãos [LISTA] foram batizados, gostaríamos de convidá-los para vir até o púlpito para que possamos fazer sua confirmação como membros de A Igreja de Jesus Cristo dos Santos dos Últimos Dias.',
    'Os irmãos [LISTA] foram batizados e confirmados membros da igreja, e gostaríamos do apoio de todos os irmãos de plena aceitação como novos membros da ala. Todos a favor, manifestem-se.',
    'Gostaríamos de chamar ao púlpito os irmãos que irão dar a bênção de apresentação das crianças [LISTA].',
    '[LISTA] Os que forem a favor, manifestem-se levantando a mão. [Pequena pausa.] Os que se opuserem, se houver, manifestem-se.',
    'Passaremos ao Sacramento, que é a parte mais importante de nossa reunião. Cantaremos como Hino Sacramental [HINO], o Sacramento será abençoado e distribuído a todos.',
    'Agradecemos a todos pela reverência durante o Sacramento. Passaremos agora a parte dos discursantes. Ouviremos primeiro o(a) irmã(o) [NOME]. Depois, ouviremos o(a) irmã(o) [NOME]. Em seguida cantaremos o hino [NOME], em pé, ao sinal do(a) regente.',
    'Gostaria de lembrar todos que estejam assistindo a transmissão da reunião, que se identifiquem para que possamos contá-los também',
    'Agradecemos a presença e participação de todos, especialmente aqueles que contribuíram de alguma forma para que essa reunião acontecesse. E convidamos todos para que estejam aqui no próximo domingo. Ouviremos como último orador o(a) irmã(o) [NOME]. Logo após, cantaremos o hino [NOME], e o(a) irmã(o) [NOME] oferecerá a última oração. Desejamos a todos uma ótima semana e que o Espírito do Senhor os acompanhe.'
),
(
    0,
    2,
    'Testemunhos',
    'Bom dia irmãos e irmãs! Gostaríamos de fazer todos muito bem vindos a mais uma Reunião Sacramental da ALA [ALA], Estaca Criciúma, neste domingo dia [DATA]. Desejamos que todos se sintam bem entre nós, especialmente aqueles que nos visitam.',
    '[NOME] está sendo desobrigado(a) como [CHAMADO]. Os que desejarem manifestar agradecimento por seus serviços prestados podem fazê-lo levantando a mão',
    '[NOME] foi chamado(a) para servir como [CHAMADO]. Os que forem a favor, manifestem-se levantando a mão. [Pequena pausa.] Os que se opuserem, se houver, manifestem-se. [Pequena pausa.]',
    'O(a) irmã(o) [NOME] foi batizado(a), gostaríamos de convidá-lo(a) para vir até o púlpito para que possamos fazer sua confirmação como membro de A Igreja de Jesus Cristo dos Santos dos Últimos Dias.',
    'O(a) irmã(o) [NOME] foi batizado e confirmado membro da igreja, e gostaríamos do apoio de todos os irmãos de plena aceitação como mais novo membro da ala. Todos a favor, manifestem-se',
    'Gostaríamos de chamar ao púlpito o irmão [NOME] que irá dar a bênção de apresentação da(o) [NOME DA CRIANÇA].',
    'É proposto que [NOME] receba o Sacerdócio de Melquisedeque e seja ordenado(a) como [CHAMADO]. Os que forem a favor, manifestem-se levantando a mão. [Pequena pausa.] Os que se opuserem, se houver, manifestem-se.',
    '[LISTA] Os que desejarem manifestar agradecimento por seus serviços prestados podem fazê-lo levantando a mão.',
    '[LISTA] Os que forem a favor, manifestem-se levantando a mão. [Pequena pausa.] Os que se opuserem, se houver, manifestem-se. [Pequena pausa.]',
    'Os irmãos [LISTA] foram batizados, gostaríamos de convidá-los para vir até o púlpito para que possamos fazer sua confirmação como membros de A Igreja de Jesus Cristo dos Santos dos Últimos Dias.',
    'Os irmãos [LISTA] foram batizados e confirmados membros da igreja, e gostaríamos do apoio de todos os irmãos de plena aceitação como novos membros da ala. Todos a favor, manifestem-se.',
    'Gostaríamos de chamar ao púlpito os irmãos que irão dar a bênção de apresentação das crianças [LISTA].',
    '[LISTA] Os que forem a favor, manifestem-se levantando a mão. [Pequena pausa.] Os que se opuserem, se houver, manifestem-se.',
    'Passaremos ao Sacramento, que é a parte mais importante de nossa reunião. Cantaremos como Hino Sacramental [HINO], o Sacramento será abençoado e distribuído a todos.',
    'Agradecemos a todos pela reverência durante o Sacramento. Hoje é nossa reunião de Jejum e Testemunhos. Gostaríamos de convidar todos a prestar seus testemunhos de forma breve e direta, dando assim tempo para que o máximo de irmãos tenham este privilégio.',
    'Gostaria de lembrar todos que estejam assistindo a transmissão da reunião, que se identifiquem para que possamos contá-los também',
    'Agradecemos a presença e participação de todos, especialmente aqueles que contribuíram de alguma forma para que essa reunião acontecesse. E convidamos todos para que estejam aqui no próximo domingo. Cantaremos o último hino [NOME] e o(a) irmã(o) [NOME] oferecerá a última oração.'
);

-- Índices
CREATE INDEX IF NOT EXISTS idx_atas_ala_id ON atas(ala_id);
CREATE INDEX IF NOT EXISTS idx_atas_data ON atas(data);
CREATE INDEX IF NOT EXISTS idx_atas_tipo ON atas(tipo);
CREATE INDEX IF NOT EXISTS idx_sacramental_ata_id ON sacramental(ata_id);
CREATE INDEX IF NOT EXISTS idx_batismo_ata_id ON batismo(ata_id);
CREATE INDEX IF NOT EXISTS idx_unidades_ala_id ON unidades(ala_id);
CREATE INDEX IF NOT EXISTS idx_unidades_estaca_id ON unidades(estaca_id);

COMMIT;
PRAGMA foreign_keys = OFF;

ALTER TABLE unidades ADD COLUMN recepcionista TEXT;
ALTER TABLE unidades ADD COLUMN pianista TEXT;
ALTER TABLE unidades ADD COLUMN regente_musica TEXT;

-- Add programa JSON column to batismo to store structured program (batizados with batizadores, confirmacoes with confirmadores, etc.)
ALTER TABLE batismo ADD COLUMN programa TEXT;

-- Secretários da ala
ALTER TABLE unidades ADD COLUMN secretario_1 TEXT;
ALTER TABLE unidades ADD COLUMN secretario_2 TEXT;
ALTER TABLE unidades ADD COLUMN secretario_3 TEXT;
ALTER TABLE unidades ADD COLUMN secretario_4 TEXT;

-- Tabela de tarefas da secretaria
CREATE TABLE IF NOT EXISTS tarefas (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    titulo TEXT NOT NULL,
    concluida INTEGER NOT NULL DEFAULT 0,
    responsavel TEXT,
    data_prevista TEXT,
    concluida_em TEXT,
    criada_em TEXT NOT NULL DEFAULT (datetime('now')),
    ala_id INTEGER NOT NULL,
    role TEXT NOT NULL DEFAULT '',
    FOREIGN KEY(ala_id) REFERENCES users(id)
);

CREATE INDEX IF NOT EXISTS idx_tarefas_ala_id ON tarefas(ala_id);
CREATE INDEX IF NOT EXISTS idx_tarefas_concluida ON tarefas(concluida);

-- NOTA DE SEGURANÇA: as colunas de auth em users (ala_id, role, display_name),
-- a tabela ala_keys e os logins por role NÃO são criados aqui (nem com credenciais
-- fixas). Tudo isso é criado de forma idempotente em DbContext.cs
-- (DatabaseInitializer) com senhas e chaves de convite ALEATÓRIAS, exibidas uma
-- única vez no console de inicialização.