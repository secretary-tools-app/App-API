# AtasApi — API REST em C# .NET 8

Portagem completa do `app.py` (Flask/Python) para ASP.NET Core seguindo o padrão
**Controller → Service → Repository** com SQLite via Dapper.

---

## Estrutura do projeto

```
AtasApi/
├── Controllers/
│   └── Controllers.cs          AuthController, AtasController, SacramentalController,
│                               BatismoController, DiscursantesController,
│                               ConfiguracoesController
├── Services/
│   └── Services.cs             AuthService, AtaService, SacramentalService,
│                               BatismoService, TemplateService, UnidadeService
│                               + JsonFieldHelper (helper de serialização)
├── Repositories/
│   └── Repositories.cs         AtaRepository, SacramentalRepository,
│                               BatismoRepository, TemplateRepository,
│                               UnidadeRepository, UserRepository
├── Models/
│   └── Entities.cs             User, Ata, Sacramental, Batismo, Estaca,
│                               Unidade, Template  (1:1 com o schema SQL)
├── DTOs/
│   └── Dtos.cs                 Requests e Responses fortemente tipados
├── Data/
│   └── DbContext.cs            SqliteDbContext (IDbContext) + DatabaseInitializer
├── Middleware/
│   └── JwtService.cs           Geração de JWT
│   └── WerkzeugHasher.cs       Verificação de hashes scrypt/pbkdf2 do Flask
├── Configuration/
│   └── AppSettings.cs          POCOs para appsettings.json
├── Program.cs                  Composição DI + pipeline HTTP
├── AtasApi.csproj              Pacotes NuGet
└── appsettings.json            Configuração (JWT secret, connection string…)
```

---

## Mapa de endpoints (equivalência com o app.py)

| Método | Rota .NET                              | Rota Flask original              | Descrição                                  |
|--------|----------------------------------------|----------------------------------|--------------------------------------------|
| POST   | `/api/auth/login`                      | `/ (POST)`                       | Login — retorna JWT                        |
| POST   | `/api/auth/logout`                     | `/logout`                        | Logout (stateless)                         |
| GET    | `/api/atas?mes=YYYY-MM`                | `/atas/mes/<mes>`                | Atas do mês                                |
| GET    | `/api/atas/all`                        | `/atas`                          | Todas as atas da ala                       |
| GET    | `/api/atas/{id}`                       | `/ata/<id>`                      | Uma ata pelo ID                            |
| GET    | `/api/atas/by-data?data=&tipo=`        | (inline nas views)               | Verifica duplicata por data+tipo           |
| POST   | `/api/atas`                            | `/ata/nova (POST)`               | Criar ata                                  |
| PUT    | `/api/atas/{id}`                       | `/ata/form (POST, editar=id)`    | Atualizar ata                              |
| DELETE | `/api/atas/{id}`                       | `/deletar_ata (POST)`            | Excluir ata + detalhes (cascade)           |
| GET    | `/api/sacramental/{ataId}`             | `api_client.get_sacramental()`   | Dados sacramentais                         |
| POST   | `/api/sacramental`                     | `api_client.create_sacramental()`| Criar sacramental                          |
| PUT    | `/api/sacramental/{ataId}`             | `api_client.update_sacramental()`| Atualizar sacramental                      |
| DELETE | `/api/sacramental/{ataId}`             | (cascade do deletar_ata)         | Apagar sacramental                         |
| GET    | `/api/batismo/{ataId}`                 | `api_client.get_batismo()`       | Dados de batismo                           |
| POST   | `/api/batismo`                         | `api_client.create_batismo()`    | Criar batismo                              |
| PUT    | `/api/batismo/{ataId}`                 | `api_client.update_batismo()`    | Atualizar batismo                          |
| DELETE | `/api/batismo/{ataId}`                 | (cascade do deletar_ata)         | Apagar batismo                             |
| POST   | `/api/discursantes/salvar`             | `/discursantes_temas/salvar`     | Salvar discursantes e hinos da semana      |
| GET    | `/api/discursantes/state?date=`        | `/api/discursantes_state`        | Estado atual (polling)                     |
| GET    | `/api/discursantes/recentes`           | `get_discursantes_recentes()`    | Últimos discursantes para autocomplete     |
| GET    | `/api/configuracoes/templates`         | `/configuracoes`                 | Templates da ala (clona padrão se vazio)   |
| GET    | `/api/configuracoes/templates/{id}`    | `/configuracoes/template/<id>`   | Um template                                |
| POST   | `/api/configuracoes/templates`         | `/configuracoes/template/criar`  | Criar template                             |
| PUT    | `/api/configuracoes/templates/{id}`    | `/configuracoes/template/<id>/salvar` | Salvar template                      |
| DELETE | `/api/configuracoes/templates/{id}`    | `/configuracoes/template/<id>/apagar` | Apagar template                      |
| GET    | `/api/configuracoes/unidade`           | `api_client.get_unidade_minha()` | Dados da unidade/ala                       |
| PUT    | `/api/configuracoes/unidade`           | `/configuracoes/ala/salvar`      | Salvar configurações da ala                |
| GET    | `/api/configuracoes/estatisticas`      | (inline em configuracoes())      | Contagens de atas                          |

---

## Campos JSON armazenados no banco

O SQLite armazena várias listas como strings JSON. A API **sempre desserializa** esses
campos no response e **serializa** no request. Os campos são:

| Tabela       | Coluna                 | Tipo real no .NET        | Exemplo de valor no banco                          |
|--------------|------------------------|--------------------------|-----------------------------------------------------|
| sacramental  | `anuncios`             | `List<string>`           | `["Anúncio 1","Anúncio 2"]`                        |
| sacramental  | `hinos`                | `string[2]` [ab., enc.] | `["Hino 96","Hino 4"]`                             |
| sacramental  | `oracoes`              | `string[2]` [ab., enc.] | `["Irmão João","Irmã Maria"]`                      |
| sacramental  | `desobrigacoes`        | `List<string>`           | `["Irmão Silva – Bispo"]`                          |
| sacramental  | `apoios`               | `List<string>`           | `["Irmã Ana – Pres. Soc. Socorro"]`               |
| sacramental  | `confirmacoes_batismo` | `List<string>`           | `["Fulano de Tal"]`                                |
| sacramental  | `apoio_membros`        | `List<string>`           | `["Ciclano"]`                                      |
| sacramental  | `bencao_criancas`      | `List<string>`           | `["Bebe Silva"]`                                   |
| sacramental  | `reconhecemos_presenca`| `List<string>`           | `["Fulano","Beltrano"]`                            |
| batismo      | `batizados`            | `List<BatizadoItem>`     | `[{"nome":"João","batizador":"Irmão X"}]`          |
| batismo      | `programa`             | `ProgramaBatismoDto`     | JSON complexo com preludio, hinos, confirmacoes…   |

---

## Autenticação

Todos os endpoints (exceto `/api/auth/login`) exigem o header:
```
Authorization: Bearer <token>
```

O `alaId` (equivalente ao `session['user_id']` do Flask) é extraído do claim `sub` do JWT.
Não é necessário passar `alaId` nos requests — ele vem sempre do token.

---

## Regra do usuário "Obra" (acesso restrito)

O `restrict_obra_user()` do Flask é implementável como um **policy handler** no .NET:

```csharp
// Em Program.cs, adicionar após AddAuthorization():
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("NotObra", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.FindFirst("username")?.Value != "Obra"));
});

// Nos controllers que precisam bloquear "Obra", decorar com:
[Authorize(Policy = "NotObra")]
```

Endpoints liberados para "Obra": apenas `/api/batismo/**` e `/api/atas` (somente tipo batismo).

---

## Como rodar

```bash
# 1. Restaurar pacotes
dotnet restore

# 2. Copiar o banco SQLite existente (ou deixar criar do zero)
mkdir -p database
cp /caminho/para/atas.db database/atas.db

# 3. Copiar o schema SQL
cp /caminho/para/schema_inicial.sql database/schema_inicial.sql

# 4. Configurar o secret JWT (NUNCA em appsettings.json versionado)
#    Opção A — variável de ambiente:
#      export Jwt__Secret='<chave aleatória com 32+ caracteres>'
#    Opção B — arquivo appsettings.Development.json (ignorado pelo git):
#      { "Jwt": { "Secret": "<chave aleatória com 32+ caracteres>" } }
#    A API recusa iniciar com secret fraco/placeholder.

# 5. Rodar
dotnet run
```

Na primeira execução em banco novo, a API cria os logins por role (1 bispo, 2 conselheiros,
3 secretários por ala) e as chaves de convite com **senhas/chaves aleatórias**, exibidas
uma única vez no console. Guarde-as com segurança; troque depois pelo endpoint de troca
de senha (`PUT /api/auth/password`).

Swagger disponível em: `http://localhost:5000/swagger`

---

## Deploy no Railway (banco persistente)

Para que o banco não desapareça em cada deploy, o app deve usar um diretório persistente e o Railway precisa montar um volume nele.

### 1) Crie o serviço no Railway
- Serviço: `Dockerfile`
- Porta: `8080` (ou deixe o app obedecer `PORT` via variável de ambiente)
- Execute o build com o Dockerfile já configurado neste projeto

### 2) Crie um volume no Railway
- Nome do volume: `atas-db`
- Mount path: `/app/data`

Esse path é o destino do SQLite (`Data Source=/app/data/atas.db`).
Se o volume estiver montado neste caminho, o arquivo do banco continua existindo entre updates e reinícios sem sobrescrever o schema do container.

### 3) Configure as variáveis de ambiente
- `ConnectionString=Data Source=/app/data/atas.db`
- `SchemaPath=database/schema_inicial.sql`
- `Jwt__Secret=<chave forte com 32+ caracteres>`

### 4) Importante
- Não use `sqlite` em memória.
- Não deixe o banco em `/tmp` nem em uma pasta do container sem volume.
- O app já está preparado para ler `PORT` do Railway e escutar essa porta dinamicamente.

---

## Dependências NuGet

| Pacote                                            | Uso                                        |
|---------------------------------------------------|--------------------------------------------|
| `Microsoft.Data.Sqlite 8.0`                       | Driver SQLite nativo para .NET             |
| `Dapper 2.1`                                      | Mapeamento SQL → objetos (micro-ORM)       |
| `Microsoft.AspNetCore.Authentication.JwtBearer`   | Validação de tokens JWT                    |
| `Microsoft.IdentityModel.Tokens`                  | Geração de chaves e credenciais            |
| `System.IdentityModel.Tokens.Jwt`                 | JwtSecurityTokenHandler                    |
| `Swashbuckle.AspNetCore`                          | Swagger UI e OpenAPI spec                  |

> **SCrypt**: o .NET 8 inclui `System.Security.Cryptography.SCrypt` nativamente.
> Não é necessário pacote externo para verificar os hashes do Flask.

---

## Notas de compatibilidade com o banco existente

1. **Hashes de senha**: werkzeug usa `scrypt:N:r:p$salt$hash`. O `WerkzeugHasher.cs`
   implementa a verificação sem necessidade de migração de senhas.

2. **Colunas de migração**: o `DatabaseInitializer` aplica automaticamente as colunas
   `discursante_1`, `discursante_2`, `outros`, `tema_1`…`obs_ultimo` caso não existam
   (equivale ao `ensure_sacramental_columns()` do Python).

3. **Campo `discursantes` (legado)**: o banco antigo usava um campo JSON `discursantes[]`.
   O novo esquema usa colunas individuais. A API lê sempre pelas colunas individuais;
   o campo legado pode ser ignorado em novos registros.

4. **Campo `hinos`**: armazena `[abertura, encerramento]`. Os campos
   `hino_sacramental` e `hino_intermediario` são colunas separadas.
   A API expande tudo em campos nomeados no response.
#   A p p - A P I  
 