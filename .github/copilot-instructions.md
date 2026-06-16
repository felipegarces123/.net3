# Copilot Instructions — Template API Bmg

> Este arquivo instrui o GitHub Copilot sobre os padrões arquiteturais, convenções de código e bibliotecas corporativas que **todo desenvolvedor deve seguir** ao trabalhar em projetos gerados por este template.

---

## 🏛️ Arquitetura: Hexagonal (Ports & Adapters)

Este projeto segue **estritamente** a arquitetura hexagonal. Existem 4 camadas com responsabilidades imutáveis:

```
Adapters/Driving/          → Entrada (HTTP, gRPC, eventos recebidos)
  Bmg.[Contexto].Api/
    Controllers/           → Recebe a requisição HTTP, delega ao AppService
    AppServices/           → Orquestra DTOs ↔ Domain Models, chama IService do Domain
    Dtos/                  → Contratos de entrada/saída da API (nunca expostos ao Domain)

Core/Domain/               → Núcleo do negócio — ZERO dependência de infraestrutura
  Bmg.[Contexto].Domain/
    Models/                → Modelos de domínio puros
    Services/              → Interfaces de serviço (Ports de entrada)
    Adapters/              → Interfaces de saída (Ports para infra: banco, APIs externas, filas)

Core/Application/          → Implementação das regras de negócio
  Bmg.[Contexto].Application/
    Services/              → Implementa as interfaces do Domain, usa os Ports de saída

Adapters/Driven/           → Saída (banco de dados, integrações externas, filas)
  Bmg.[Contexto].Database/ → Repositories com Dapper, UnitOfWork
  Integrations/            → Implementa os Ports de integração definidos no Domain
```

---

## 📐 Regras que o Copilot deve sempre seguir

### 1. Dependências entre camadas (NUNCA violar)

| De | Pode depender de | NUNCA depender de |
|----|---|---|
| `Domain` | Nada além de `arqc-project-utils` | `Application`, `Database`, `Api`, Entity Framework, Dapper |
| `Application` | `Domain` | `Database` direto (sempre via Port/Interface), `Api` |
| `Database` | `Domain` (interfaces/models) | `Application`, `Api` |
| `Api` (Driving) | `Domain` (interfaces), `Application` | `Database` direto |

### 2. Nomenclatura de classes

```
IConsigBoilerplateService      → Port de entrada, em Domain/Services/v{n}/
ConsigBoilerplateService       → Implementação, em Application/Services/v{n}/
IConsigBoilerplateAppService   → Interface do AppService, em Api/AppServices/v{n}/Interfaces/
ConsigBoilerplateAppService    → AppService, em Api/AppServices/v{n}/
ConsigBoilerplateController    → Controller, em Api/Controllers/v{n}/
IWeatherRepository           → Port de saída, em Domain/Adapters/
WeatherRepository            → Implementação, em Database/Repositories/v{n}/
```

### 3. Versionamento obrigatório

Toda interface, service, controller, DTO e repository **deve estar em subpasta `v{n}/`**.  
Ao criar uma breaking change, crie `v2/` — nunca altere o contrato existente.

### 4. AppService — responsabilidade exclusiva

O `AppService` é a **única** classe que:
- Converte DTO de entrada → Domain Model (via `Mapper`)
- Chama o `IService` do Domain
- Converte Domain Model → DTO de resposta (via `Mapper`)

```csharp
// ✅ CORRETO
public async Task<WeatherResponse> PostAsync(WeatherRequest request, CancellationToken cancellationToken)
{
    var model = Mapper.Map<WeatherModel>(request);
    var result = await Service.CreateWeatherAsync(model, cancellationToken);
    return Mapper.Map<WeatherResponse>(result);
}

// ❌ ERRADO — regra de negócio no AppService
public async Task<WeatherResponse> PostAsync(WeatherRequest request, CancellationToken cancellationToken)
{
    if (request.Temperature > 100) throw new Exception("...");  // regra pertence ao Domain
    ...
}
```

### 5. Controller — responsabilidade exclusiva

O `Controller` **apenas** recebe a requisição, delega ao `AppService` e trata o retorno HTTP.

```csharp
// ✅ CORRETO
public async Task<ActionResult<WeatherResponse>> GetAsync(long id, CancellationToken cancellationToken)
{
    var result = await AppService.GetAsync(id, cancellationToken);
    if (HasNotifications()) return Notifications();
    return result is null ? NoContent() : Ok(result);
}

// ❌ ERRADO — lógica de negócio no Controller
public async Task<ActionResult> PostAsync(WeatherRequest request, ...)
{
    if (!ModelState.IsValid) ...   // use FluentValidation com Validators/
    var entity = new Weather { ... }; // mapeamento pertence ao AppService
}
```

### 6. Domain Service — nunca acessa infraestrutura diretamente

```csharp
// ✅ CORRETO — Application/Services/v1/
public class ConsigBoilerplateService : BmgServiceBase, IConsigBoilerplateService
{
    private readonly IUnitOfWorkOracle _unitOfWork;   // Port de saída (interface)

    public async Task<WeatherModel> GetWeatherAsync(long id, CancellationToken ct)
    {
        var entity = await _unitOfWork.Weathers.SelectAsync(ct, id);
        return Mapper.Map<WeatherModel>(entity);
    }
}

// ❌ ERRADO — DbContext ou SqlConnection no Domain
public class ConsigBoilerplateService
{
    private readonly OracleConnection _conn;  // NUNCA: dependência de infra no Domain
}
```

### 7. Repository — usa sempre Dapper via GenericRepository

```csharp
// ✅ CORRETO
public class WeatherRepository : GenericRepository<DatabaseConnection, Weather>, IWeatherRepository
{
    public WeatherRepository(IServiceProvider provider) : base(DatabaseConnection.Oracle, provider) { }

    public override async Task<Weather> SelectAsync(CancellationToken ct, params object[] ids)
    {
        var builder = new SqlBuilder();
        var template = builder.AddTemplate("SELECT * FROM schema.tbl_wth /**where**/");
        builder.Where("wth_id = @Id");
        return await Connection.QueryFirstOrDefaultAsync<Weather>(template.RawSql, new { Id = ids[0] });
    }
}

// ❌ ERRADO
public class WeatherRepository
{
    private readonly DbContext _context;       // Entity Framework é proibido
    public async Task<Weather> GetById(int id) => await _context.Weathers.FindAsync(id);
}
```

---

### 8. Domain Model vs Database Entity — regra anti-duplicidade (Sonar)

> **Dois modos válidos — escolha UM, nunca os dois:**
> 1. **Entity separada** (o modo do template de amostra `Weather`/`WeatherModel`): existe `*Entity` em `Database/Entities` **e** os 4 maps padrão de `ModelMappingProfile` (`Model↔Entity`, `PaginatedData`, `Operation`, `JsonPatchDocument`, todos com `ReverseMap()`). Use quando o Model tem comportamento, ou a tabela tem campos de infra sem equivalente no Model.
> 2. **Model como `TEntity`** (anti-duplicidade): quando Model e Entity são idênticos, **não** crie a Entity nem o `Model↔Entity` map — use o Domain Model diretamente como `TEntity` do `GenericRepository`.
>
> ⚠️ O erro a evitar é o **híbrido**: ter `*Entity` idêntica ao `*Model` *e* um `CreateMap` entre elas só para converter — isso é a duplicidade que o Sonar reporta.

**A maior fonte de duplicidade de código no Sonar** em APIs desta stack é criar uma classe `*Entity` no projeto `Database` com as mesmas propriedades do `*Model` do Domain, e depois usar AutoMapper para converter entre elas.

#### Regra: use o Domain Model diretamente como `TEntity` do repositório

O `GenericRepository<TConnection, TEntity>` aceita **qualquer tipo** como `TEntity` — incluindo o próprio Domain Model. Quando as propriedades são as mesmas, **não crie uma classe Entity separada**.

```csharp
// ✅ CORRETO — Domain Model usado diretamente como TEntity
// Sem classe Weather em Database/Entities — elimina duplicidade

public interface IWeatherRepository 
    : IGenericRepository<DatabaseConnection, WeatherModel> { }

public class WeatherRepository 
    : GenericRepository<DatabaseConnection, WeatherModel>, IWeatherRepository
{
    public WeatherRepository(IServiceProvider provider) 
        : base(DatabaseConnection.WeatherDb, provider) { }
}

// Application/Mappings — NÃO precisa de ModelMappingProfile para Model ↔ Entity
// Se houver mapeamento de coluna, use [Column] no próprio Domain Model:
// [Column("wth_temperature")] public decimal Temperature { get; set; }
```

```csharp
// ❌ ERRADO — duplicidade que o Sonar irá reportar
// Domain/Models/WeatherModel.cs — 20 propriedades
public class WeatherModel { public string City { get; set; } /* ... */ }

// Database/Entities/WeatherEntity.cs — as mesmas 20 propriedades → DUPLICIDADE
public class WeatherEntity { public string City { get; set; } /* ... */ }

// Application/Mappings/ModelMappingProfile.cs — AutoMapper desnecessário
CreateMap<WeatherModel, WeatherEntity>().ReverseMap(); // ← origem da violação Sonar
```

#### Quando criar uma Entity separada é justificado

| Situação | Separar? | Motivo |
|---|---|---|
| Propriedades idênticas entre Model e Entity | ❌ Não | Duplicidade sem ganho — use Model como TEntity |
| Domain Model tem métodos / lógica de negócio | ✅ Sim | Entity deve ser anêmica; Model tem comportamento |
| Nomes de colunas do banco divergem das propriedades | ❌ Não | Use `[Column("nome_coluna")]` no Domain Model |
| Domain Model é exposto diretamente como DTO na API | ✅ Sim | Neste caso, separar Model de Entity é correto |
| Tabela tem campos de infra (audit, softdelete) sem equivalente no Model | ✅ Sim | Entity carrega campos que não pertencem ao domínio |

---

## 🚫 Regras críticas de geração (evita os erros mais comuns)

### 9. Tokens de substituição — nada de nomes do template no código gerado

Este template usa `ConsigBoilerplate` como nome de exemplo e `weather-forecast-api`/`wfcst` como amostra. Ao gerar um serviço real, substitua **todos** os tokens de forma consistente — solution, projetos, namespaces, classes, **métodos de DI** (`Add{App}ApiModule()`…) e metadados do `.csproj`.

> ⛔ **NUNCA** deixe `ConsigBoilerplate`, `Weather`, `wfcst`, `weather-forecast-api` nem o typo `Wather` em qualquer identificador, namespace, nome de arquivo ou metadado gerado.

### 10. `using` canônicos — NÃO inventar namespaces

Use **somente** namespaces deste catálogo e dos projetos do próprio serviço (`Bmg.{App}.*`). Os seguintes **NÃO EXISTEM** e são proibidos: `Bmg.Infra.Database`, `Bmg.Infra.Database.Repositories`, `Bmg.Commons.Logging`, `Bmg.Commons.Tracing`, `Bmg.Infrastructure.Api.Controllers`, `Bmg.Infrastructure.Observability.Attributes`.

| Base / tipo / atributo | Namespace correto |
|---|---|
| `BmgControllerBase<I>`, `BmgAppServiceBase<I>`, `BmgServiceBase` | `Bmg.Project.Utils.Base` |
| `[BmgDynatraceTrace]` | `Bmg.Logging.Internal.Attributes` |
| `PaginatedData<T>`, `Operation<T>` | `Bmg.Project.Utils.Data` |
| `IBmgServiceBase` | `Bmg.Project.Utils.Interfaces` |
| `GenericRepository<,>`, `IGenericRepository<,>`, `SqlBuilder` | `Bmg.Connection.Manager.Data` |
| `DatabaseConnection` / `DatabaseNoSqlConnection` | `Bmg.{App}.Domain` |
| `ILogger<T>` | `Microsoft.Extensions.Logging` |

### 11. Interface × implementação — arquivos e pastas separados (BLOQUEIA A PIPELINE BMG)

A pipeline de publicação **rejeita** interface e classe concreta no mesmo arquivo. Um arquivo = um tipo público:

| Componente | Interface | Implementação |
|---|---|---|
| AppService | `Api/AppServices/v{n}/Interfaces/I{Nome}AppService.cs` | `Api/AppServices/v{n}/{Nome}AppService.cs` |
| Repository | `Database/Repositories/Interfaces/v{n}/I{Nome}Repository.cs` | `Database/Repositories/v{n}/{Nome}Repository.cs` |
| Service (port) | `Domain/Services/v{n}/I{Nome}Service.cs` | `Application/Services/v{n}/{Nome}Service.cs` |

### 12. Sem `Initialize()` em Controller e AppService

`Controller` e `AppService` **não** declaram construtor nem método `Initialize()`. O acesso à dependência vem da classe base: `AppService` em `BmgControllerBase<I>`, `Service` em `BmgAppServiceBase<I>`. Apenas o **Service** da Application recebe dependências por construtor.

### 13. Método assíncrono — sufixo `Async` obrigatório

Todo método que retorna `Task`/`Task<T>` **deve** terminar com `Async`, em qualquer camada (interface, service, appservice, controller, repository).

### 14. Repository retorna a Entity (não o Model)

O repositório e seus métodos retornam a **Entity** (`Weather`, `Usuario`) — nunca o `*Model`. A conversão Entity → Model acontece no Application Service via `Mapper.Map<{Nome}Model>(entity)`. (Exceção: regra anti-duplicidade, quando o Model é usado como `TEntity`.)

### 15. Registro de DI obrigatório para toda feature nova

```csharp
// Api/...ApiDependency.cs
services.AddScoped<AppServices.v1.Interfaces.I{Nome}AppService, AppServices.v1.{Nome}AppService>();
// Application/...ApplicationDependency.cs
services.AddScoped<Domain.Services.v1.I{Nome}Service, Services.v1.{Nome}Service>();
// Database/...DatabaseDependency.cs
services.AddBmgScopedRepository<Repositories.Interfaces.v1.I{Nome}Repository, Repositories.v1.{Nome}Repository>();
```
Registrar também o repositório no `UnitOfWorkOracle` e os profiles novos no `AddAutoMapper(...)`. Nenhum Service/AppService/Repository/Profile fica sem registro.

### 16. AutoMapper — dois profiles, sem cruzar camadas

- `Application/Mappings/v1/ModelMappingProfile.cs` → **Model ↔ Entity**. **NUNCA** referencie `*.Api.Dtos.*` aqui.
- `Api/Mappings/v1/DtoMappingProfile.cs` → **DTO ↔ Model**.

Conjunto padrão por entidade (com `ReverseMap()`), sem `.ForMember` redundante:
```csharp
CreateMap<{Nome}Model, {Entidade}>().ReverseMap();
CreateMap<PaginatedData<{Nome}Model>, PaginatedData<{Entidade}>>().ReverseMap();
CreateMap<Operation<{Nome}Model>, Operation<{Entidade}>>().ReverseMap();
CreateMap<JsonPatchDocument<{Nome}Model>, JsonPatchDocument<{Entidade}>>().ReverseMap();
```

---

## 📦 Bibliotecas corporativas — Classificação e uso

### 🔴 Obrigatórias — toda API deve usar


#### `arqc-project-utils` → `Bmg.Project.Utils`
- **`AddBmgApiProjectDependencies(builder)`** — único ponto de bootstrap: convenciona nome do serviço, prefixo de rota (`/v{version}/{service}`), rate limit por IP, Swagger/OpenAPI, `/healthz` (probe EKS), autenticação base, controllers e Kestrel
- **`BmgControllerBase`**, **`BmgAppServiceBase`**, **`BmgServiceBase`** — classes base obrigatórias (ver seção abaixo)
- **Modo Swagger no `Program.cs`** — usar guarda **local** com variável `isSwaggerMode` baseada em `Environment.GetEnvironmentVariable("SWAGGER_GENERATION")`, protegendo blocos de infraestrutura com `if (!isSwaggerMode)`
- **`PaginatedData<T>`** — resposta paginada padronizada
- Configura **FluentValidation**, **ApiVersioning** e **propagação de headers** corporativos automaticamente

#### `arqc-api-client` → `Bmg.Api.Client`
- Encapsula **Flurl** para consumo HTTP/SOAP de APIs internas BMG
- Configura timeout, encoding, HTTP e SOAP de forma padronizada
- **`BmgTraceLogHandler`** — injeta e propaga o correlation id `x-bmg-id` em toda requisição de saída; gera logs estruturados de request/response
- **Rate limit integrado** com `arqc-project-utils` — controla concorrência entre requisições externas
- Suporte a **API** (via `HeaderPropagation`) e **Worker** (via `BmgIdHeaderHandler`) no mesmo pacote
- Uso: registrar o client em `Program.cs` com `.AddBmgApiClient<IMinhaApi, MinhaApi>()`

#### `arqc-auth` → `Bmg.Auth`
- Autenticação/autorização **JWT corporativa** do banco digital
- **Obrigatório** em todo Controller exposto — sem ele, a API não valida tokens Bmg
- Integrado automaticamente via `AddBmgApiProjectDependencies`; atributo `[Authorize]` ativo no `BmgControllerBase`

#### `arqc-connection-manager` → `Bmg.Connection.Manager`
- Gerenciamento de conexões DB relacionais
- **`GenericRepository<T>`** — repositório genérico com Dapper (`QueryAsync`, `QueryFirstOrDefaultAsync`, `SqlBuilder`)
- **`IUnitOfWork`** / **`DatabaseConnection`** — abstração de transações e abertura de conexão

#### `arqc-parameters-manager` → `Bmg.Parameter.Manager`
- Configuração dinâmica via plataforma **CNFG** do BMG
- Substitui variáveis de ambiente para parâmetros sensíveis/dinâmicos (strings de conexão, feature flags, timeouts)
- Atualiza valores em tempo de execução sem redeploy

#### `arqc-logging-internal` → `Bmg.Logging.Internal`
- Logging estruturado com rastreabilidade Dynatraceado
- **`[BmgDynatraceTrace]`** — atributo obrigatório **na declaração da classe** de `Service` e `AppService` (a anotação na classe cobre todos os métodos públicos; **NUNCA** repita o atributo nos métodos)
- Integrado com o pipeline de observabilidade do banco (Dynatrace + ELK)

### 🟡 Não obrigatórias — extremamente recomendadas quando o serviço usa a tecnologia

| Biblioteca | Pacote NuGet | Quando usar |
|---|---|---|
| `arqc-kafka` | `Bmg.Kafka` | Integração Kafka Producer/Consumer |
| `arqc-nosqlconnection-manager` | `Bmg.NoSqlConnection.Manager` | Gerenciamento de conexões NoSQL — MongoDB (padrão), Atlas e demais bancos de documentos. **Obrigatória** quando o serviço usa NoSQL |
| `arqc-cache-manager` | `Bmg.Cache.Manager` | Cache distribuído (Redis) e local (MemoryCache) |
| `arqc-storage-manager` | `Bmg.Storage.Manager` | Storage de arquivos — integração com S3 |
| `arqc-notification-manager` | `Bmg.Notification.Manager` | Integração com ADCN (Central de Notificações) |
| `arqc-bind-converter` | `Bmg.Bind.Converter` | Conversão de arquivos `.docx` e HTML em PDF |
| `arqc-call-orchestrator` | `Bmg.Call.Orchestrator` | Orquestra requisições externas controlando rate limit e retries |
| `arqc-file-functions-lib` | `Bmg.File.Functions` | Manipulação de arquivos Texto, CSV, JSON, XML e binário — muito recomendado para batchs |
| `arqc-crypto` | `Bmg.Crypto` | Encrypt/Decrypt de dados seguindo recomendações de SecOps |

### 🔵 Disponíveis para casos de exceção de tecnologia

| Biblioteca | Pacote NuGet | Quando usar |
|---|---|---|
| `arqc-process-dlq` | `Bmg.Process.Dlq` | Processar mensagens represadas em tópico DLQ do Kafka |
| `arqc-queue-service` | `Bmg.Queue.Service` | Integração com SQS da AWS |
| `arqc-rabbitmq` | `Bmg.RabbitMq` | Integração com mensageria RabbitMQ |

> **Nunca use**: `MediatR` (ADR-003) e `ErrorOr` (ADR-004) — **não são permitidos** nesta stack.  
> **Acesso a dados**: prefira **Dapper** via `GenericRepository` (`arqc-connection-manager`). Entity Framework pode ser usado quando justificado, mas não é o padrão recomendado.

---

## 🔑 Classes base obrigatórias

| Classe sua | Deve herdar de |
|---|---|
| Application Service | `BmgServiceBase` |
| AppService (Driving) | `BmgAppServiceBase<IService>` |
| Controller | `BmgControllerBase<IAppService>` |
| Repository | `GenericRepository<TConnection, TEntity>` |

---

## 🔄 Geração de Swagger (contrato OpenAPI) — API First

O contrato OpenAPI é gerado **automaticamente no build** quando `SWAGGER_GENERATION=true`.  
A geração deve vir da `Bmg.Project.Utils` via `buildTransitive` (sem configuração local de geração no `.csproj`, como `Microsoft.Extensions.ApiDescription.Server`, `GenerateOpenApiFiles`, `OpenApiDocument` e targets de rename/cache).

**Saída padrão (ordem de precedência):**
1. `$(BUILD_ARTIFACTSTAGINGDIRECTORY)/swagger-specs` (pipeline)
2. `$(SolutionDir)/swagger-specs` (build via `.sln`)
3. `$(MSBuildProjectDirectory)/swagger-specs` (build via `.csproj`)

**Arquivos esperados:**
- `swagger-v*.json` (por versão)
- `swagger.json` (alias da maior versão)

### Como gerar localmente (conferência antes do PR)

```bash
# Na raiz da solution do serviço:
SWAGGER_GENERATION=true dotnet build
# → swagger-specs/swagger-v*.json e swagger-specs/swagger.json gerados automaticamente
```

### Como funciona na pipeline (quality-gate)

```yaml
- SWAGGER_GENERATION=true dotnet build --no-restore
# A pasta swagger-specs é publicada como artefato e enviada ao Sensedia Adaptive Governance
```

### Regra obrigatória ao registrar novos módulos de infraestrutura no `Program.cs`

Todo módulo que depende de infraestrutura real (banco, broker, CNFG, APIs externas) **deve ser guardado**  
com variável local `isSwaggerMode` para não bloquear a geração do contrato:

```csharp
// ✅ CORRETO — módulo de infra guardado
var isSwaggerMode = string.Equals(
    Environment.GetEnvironmentVariable("SWAGGER_GENERATION"),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (!isSwaggerMode)
{
    builder.Services.AddMeuServicoDatabase(builder.Configuration);
    builder.Services.AddBmgKafka(...);
}

// ❌ ERRADO — módulo de infra sem guard quebra a geração do swagger na pipe
builder.Services.AddMeuServicoDatabase(builder.Configuration);
```

**O que NÃO deve ser guardado** (necessário para o Swagger funcionar):
- `AddAutoMapper` — necessário para mapeamentos nos DTOs
- `Add{App}ApiModule()` — registra Controllers, AppServices e interfaces
- `Add{App}ApplicationModule()` — registra Services do Domain
- `AddBmgApiProjectDependencies` — configura Swagger, versioning e middleware

---

## 🏛️ Governança de APIs — Padrões Bmg / Sensedia Adaptive Governance

> Base de conhecimento obrigatória para construção de contratos OpenAPI que atendam **API First**, métricas de maturidade Sensedia e os padrões institucionais do Bmg.

### 📛 Nome da API

| Conceito | Regra |
|---|---|
| **Nome lógico** (`info.title`) | Derivado da Arquitetura Funcional — representa o domínio de negócio, não detalhes técnicos |
| **Nome físico** (exposição no gateway) | `sigla-nome-servico-api` em kebab-case — ex: `abcd-clientes-api` |
| Geração automática | `Bmg.Project.Utils` monta o nome físico automaticamente mantendo kebab-case |

- ✅ `API de Crédito Consignado` / `abcd-credito-consignado-api`
- ❌ `ms-credito-consignado-v1`, `bmg-api-credito-pj`, `JavaCustomerAPI`

### 🔢 Versionamento

**Lógico** (`info.version`): apenas o número MAJOR (`1`, `2`, `3`) — sem `MAJOR.MINOR.PATCH` no contrato.  
**Físico** (URI): `vN` precedendo o endpoint — ex: `/v1/clientes`, `/v3/relacionamento-clientes`.

| Causa breaking change (→ novo MAJOR) | Não é breaking change |
|---|---|
| Remover ou renomear endpoint | Adicionar campos opcionais |
| Alterar URI ou método HTTP | Criar novos endpoints |
| Alterar tipo de dado | Adicionar novos códigos de erro |
| Tornar campo obrigatório no request | Refatorar código interno |
| Mudança de semântica / regra de negócio | Corrigir bugs / ajustar docs |

**Cadeia de identidade Bmg** — o mesmo `sigla-nome-servico-api` deve ser consistente em:
1. Repositório Azure DevOps
2. DNS do ambiente (`sigla-nome-servico-api.cloudbmg.app.br`)
3. `basePath` da API (`/vN/nome-servico`)

### 🌐 URIs

- Representam **recursos de negócio**, nunca ações ou verbos (`/clientes`, não `/buscarClientes`)
- **Plural** para coleções (`/clientes`, `/cartoes`); **singular** para elemento único (`/perfil`)
- Apenas letras **minúsculas**, separadas por **kebab-case** (`/relacionamento-clientes`)
- **Máximo 3 níveis** de hierarquia: `/recurso/{id}/sub-recurso/{id}/sub-sub-recurso`
- QueryParams e PathParams: **kebab-case** (`?id-usuario=`, `/{id-cliente}`)
- ❌ Proibido: verbos, formato de mídia (JSON, XML) na URI, versão no nome da API (`ClientesAPIv2`)

### 📦 Payloads

- Representam **dados do recurso**, não comandos (`"nome": "João"` ✅ vs `"acao": "CADASTRAR"` ❌)
- Nomenclatura de propriedades: **camelCase** — ex: `dataNascimento`, `numeroDocumento`
- ❌ Evitar: `dt_nasc`, `docNum`, `snake_case`, abreviações
- **Formato de datas**: ISO 8601 — `YYYY-MM-DD` / `YYYY-MM-DDTHH:MM:SSZ` (sempre UTC)
- Todo atributo deve ter `type`, `description`, `example` e, quando aplicável, `minLength`/`maxLength`
- Campos somente leitura: `readOnly: true`; somente escrita: `writeOnly: true`
- Campos anuláveis: `nullable: true` explícito

### 🗂️ Entidades (schemas)

- Nome das entidades: **PascalCase**, coeso ao negócio (`Cliente`, `ContaDigital` ✅ vs `ClienteFinanceiroOperacional` ❌)
- **Entidades canônicas** (reutilizáveis entre operações) são preferidas — defini-las em `components/schemas`
- **Entidades não canônicas** (request/response específicos) devem indicar propósito no nome: `AlteracaoCotacaoRequest`
- ENUMs: **lower-kebab-case** com termos claros de negócio (`status-cotacao`, `tipo-conta`)
- Entidades usadas apenas inline (não reutilizadas) não precisam ir para `components/schemas`

### 📋 HTTP Status Codes — uso semântico correto

| Situação | Status correto |
|---|---|
| Sucesso GET/PUT/PATCH com body | `200` |
| Recurso criado (POST) | `201` |
| Resposta paginada | `206` |
| Sem conteúdo (DELETE) | `204` |
| Request quebrado (campos obrigatórios, tipos errados) | `400` |
| Autenticação ausente/inválida | `401` |
| Sem permissão | `403` |
| Recurso não encontrado (URI específica) | `404` |
| Regra de negócio / validação semântica violada | `422` |
| Erro interno | `500` |
| Timeout no Gateway | `504` |

> **Regra crítica**: `400` ≠ `422`. Use `400` para contrato quebrado e `422` para validação de negócio.  
> Coleções vazias em filtros (ex: `GET /clientes?uf=SP`) → `200` com lista vazia, **nunca** `404`.

### 📄 Headers

- Headers HTTP padrão: **kebab-case com iniciais maiúsculas** — `Content-Type`, `Authorization`, `Cache-Control`
- Headers custom: **kebab-case com iniciais maiúsculas**, sem prefixo `x-` obrigatório — ex: `Sistema-Operacional`
- **`x-bmg-id`**: correlation id obrigatório — injetado automaticamente pelo `Bmg.Api.Client` via `BmgTraceLogHandler`; substitui `Correlation-Id` / `x-correlation-id` em todo o ecossistema Bmg
- Todo header no contrato deve ter `description`, `example` e indicação de `required: true/false`

### 📑 Paginação

- Controle exclusivamente por **QueryParams**: `_offset` (posição inicial) e `_limit` (quantidade máxima)
- Respostas paginadas retornam **`206 Partial Content`** (inclusive na última página)
- Response body: usar atributo `items` para a lista + metadados de paginação na raiz — não retornar array na raiz
- Suporte a filtros: igualdade (`?status=paid`), busca parcial (`?name=*Cereal*`), numérico (`?items=gt:10`), data ISO 8601
- Ordenação: `_asc=campo` e `_desc=campo`

### 🔁 Idempotência

- `GET`, `PUT`, `DELETE` são idempotentes por natureza
- `POST` não é idempotente — quando necessário, usar header `Idempotency-Key`
- `PATCH` recomenda-se garantir idempotência na implementação; caso contrário, usar `Idempotency-Key`

### 🔗 HATEOAS (uso restrito)

- **Não obrigatório** — adotar apenas quando houver ganho real para o consumidor
- Links agrupados em `_links`; criar entidade `Link` em `components/schemas`
- Em GET paginado com HATEOAS: retornar `self`, `first`, `last`

---

## ✅ Checklist ao criar qualquer novo caso de uso

- [ ] Interface do serviço em `Domain/Services/v{n}/I{Nome}Service.cs` herdando `IBmgServiceBase`
- [ ] Implementação em `Application/Services/v{n}/{Nome}Service.cs` herdando `BmgServiceBase`
- [ ] Interface do AppService em `Api/AppServices/v{n}/Interfaces/I{Nome}AppService.cs`
- [ ] AppService em `Api/AppServices/v{n}/{Nome}AppService.cs` herdando `BmgAppServiceBase<I{Nome}Service>`
- [ ] Controller em `Api/Controllers/v{n}/{Nome}Controller.cs` herdando `BmgControllerBase<I{Nome}AppService>`
- [ ] DTOs de request/response em `Api/Dtos/v{n}/{Nome}/`
- [ ] Atributo `[BmgDynatraceTrace]` **na classe** do AppService e do Application Service (não nos métodos)
- [ ] Mapeamentos AutoMapper em `Mappings/` (tanto em Application quanto em Api)
- [ ] Interface e implementação em **arquivos/pastas separados** (AppService e Repository) — pipeline BMG bloqueia o contrário
- [ ] Registro de DI feito: AppService no `...ApiDependency`, Service no `...ApplicationDependency`, Repository no `...DatabaseDependency` + `UnitOfWorkOracle`, profiles no `AddAutoMapper`
- [ ] Todo método `Task` com sufixo `Async`; nenhum token do template (`ConsigBoilerplate`/`Weather`/`Wather`) no código
- [ ] Teste unitário espelhando a estrutura em `Tests/`
