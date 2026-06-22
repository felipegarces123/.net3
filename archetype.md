# Archetype Guide — Bmg.ConsigBoilerplate (Template API .NET BMG)

## Overview

Este é um **template de API REST corporativa** construído com **.NET 10 + ASP.NET Core**, seguindo **Arquitetura Hexagonal (Ports & Adapters)** com separação estrita de responsabilidades entre camadas. O sample que acompanha o template é uma **Weather Forecast API** (`weather-forecast-api`, prefixo `wfcst`), usada como referência de implementação de CRUD, integrações externas, mensageria e governança de contrato.

**Stack principal:**
- **Framework**: .NET 10 + ASP.NET Core Web API
- **Arquitetura**: Hexagonal (Ports & Adapters) — `Domain` ← `Application` / `Driven`, `Driving` → `Application` → `Domain`
- **Acesso a dados**: **Dapper** via `GenericRepository` + `SqlBuilder` (Oracle); `UnitOfWork` para transações
- **NoSQL**: `Bmg.NoSqlConnection.Manager` (MongoDB) — opcional
- **Mapeamento**: AutoMapper (DTO ↔ Model na API, Model ↔ Entity na Application)
- **Validação**: FluentValidation (`Validators/`)
- **HTTP externo**: `Bmg.Api.Client` (encapsula Flurl, propaga `x-bmg-id`)
- **Mensageria**: `Bmg.Kafka` (Producer/Consumer) — opcional
- **Autenticação**: `Bmg.Auth` (JWT corporativo / Entra ID, roles)
- **Configuração**: `Bmg.Parameter.Manager` (plataforma CNFG) — sem segredos em `appsettings` fora do DEV
- **Observabilidade**: `Bmg.Logging.Internal` (logs estruturados + `[BmgDynatraceTrace]`), `/healthz`
- **Bootstrap/convenções**: `Bmg.Project.Utils` (`AddBmgApiProjectDependencies`)
- **Testes**: xUnit, espelhando a árvore de produção em `Tests/`

**Quando implementar novas funcionalidades:**
1. **Siga as camadas** na ordem: `Domain → Application → Driven (Database/Integrations) → Driving (Api)`
2. **Não coloque regra de negócio** em Controller, AppService ou Repository — ela pertence ao `Application/Services`
3. **Versione tudo**: toda interface, service, controller, DTO e repository vive em subpasta `v{n}/`
4. **Adicione apenas novos arquivos** nas camadas apropriadas; não acople camadas internas a externas
5. **Referências de implementação**: `Core/Domain/.../Services/v1/IConsigBoilerplateService.cs`, `Core/Application/.../Services/v1/ConsigBoilerplateService.cs`, `Adapters/Driving/Apis/.../Controllers/v1/ConsigBoilerplateController.cs`

> Regras vinculantes adicionais para devs e para o GitHub Copilot estão em `.github/copilot-instructions.md`. Este guia é a visão narrativa; aquele arquivo é a fonte normativa.

---

## ⚠️ Regras de execução (agentes de IA e devs) — LEIA ANTES DE GERAR CÓDIGO

Este guia descreve **ações obrigatórias**, não apenas exemplos ilustrativos. Os blocos de código com cabeçalho `// EDITAR ...` ou listados em um *Checklist de arquivos* **devem ser executados** — não são opcionais.

1. **Toda etapa "EDITAR" em arquivo existente é obrigatória.** Criar as classes novas **não** conclui a tarefa: é preciso **voltar e editar** os arquivos de registro (`*Dependency.cs`, `UnitOfWorkOracle` + sua interface, `UnitOfWorkOracleContext`, `Program.cs`, `Bmg.{Servico}.Api.csproj`). Esse é o passo mais esquecido.
2. **Todo projeto novo exige o arquivo `.csproj`.** Criar apenas `*Dependency.cs` ou `*Facade.cs` **não** cria o projeto. É obrigatório criar `Bmg.{Servico}.{Nome}.csproj` e adicioná-lo à solution (`dotnet sln add ...`).
3. **A tarefa só termina quando todos os arquivos do "Checklist de arquivos" da seção correspondente** (nova entidade / nova integração) tiverem sido **criados E editados**.
4. **Crie já com o nome certo — não gere como `ConsigBoilerplate` para renomear depois.** O contexto da solução é `Bmg.{Servico}` (`{Servico}` = nome do serviço em PascalCase; no template-base aparece como `ConsigBoilerplate`). Ao **criar** qualquer projeto, classe ou namespace, use **o mesmo prefixo da solução atual** (`Bmg.{Servico}.*`) — confira o nome do `.sln`/`.csproj` existentes e siga-o. **Nunca** escreva `ConsigBoilerplate` literal num serviço com outro nome. Ver **"Nomeação e bootstrap do contexto"**.
5. **Repositório (Dapper) = par interface + implementação — sempre os dois (ver 3a).** Para uma entidade que persiste, crie `I{Entidade}Repository : IGenericRepository<DatabaseConnection, {Entidade}>` (em `Database/Repositories/Interfaces/v{n}/`) **e** `{Entidade}Repository : GenericRepository<DatabaseConnection, {Entidade}>` (em `Database/Repositories/v{n}/`). Nunca acesse o banco via EF/`DbContext` direto.
6. **Integração externa = projeto próprio + `ApiBase` + `IBmgApiClient` (ver 3b).** Toda integração é um projeto sob `Adapters/Driven/Integrations/Apis/` que implementa um port do `Domain`; a classe (`{Nome}ApiManager`/`{Nome}Facade`) **herda `ApiBase`** e **injeta `IBmgApiClient`** (`using Bmg.Api.Client;` + `using Bmg.Api.Client.Base;`) — nunca um `IApiClient` genérico. Não esqueça o `.csproj` (regra 2), o `ProjectReference` no `.Api` e o registro no `Program.cs` (regra 1).
7. **Todo repositório novo entra no UnitOfWork (ver 3d).** Exponha-o no `UnitOfWorkOracle` como `public I{Entidade}Repository {Entidades} => _provider.GetRequiredService<I{Entidade}Repository>();` **e** declare a mesma assinatura na interface `IUnitOfWorkOracle`. Adicione também `public DbSet<{Entidade}> {Entidades} { get; set; }` no `UnitOfWorkOracleContext` (scaffolding EF, `[Obsolete]`).
8. **Validação final obrigatória**: `dotnet build` (compila — prova que registros e `ProjectReference` existem) **e** `SWAGGER_GENERATION=true dotnet build` (gera o contrato).

---

## Nomeação e bootstrap do contexto (criar já com o nome certo)

**Princípio: crie já com o nome certo — não gere como `ConsigBoilerplate` para renomear depois.** O contexto da solução é `Bmg.{Servico}.*` e todo artefato novo deve nascer com esse prefixo. O rename em massa (sed) abaixo é **fallback**, não o caminho principal.

**Tokens**
- `{Servico}` — nome do serviço em **PascalCase**, sem espaços (ex.: `CreditoConsignado`). Usado em projetos, pastas, namespaces e classes: `Bmg.{Servico}.*`.
- `{sigla}` / `{servico-fisico}` — identidade **física** da aplicação: sigla (ex.: `ccsg`) e nome em **kebab-case** (ex.: `credito-consignado`). Usados em `ApplicationPrefix` / `ApplicationName` / `<AssemblyName>` / governança (`sigla-nome-servico-api`).

### Bootstrap de um serviço novo (preferido) — `dotnet new`
O template é empacotado (`Bmg.Template.Net.Api.<versao>.nupkg`, presente na raiz). Gere o serviço **já nomeado** — o motor do `dotnet new` substitui o `sourceName` na criação, então **nenhum rename manual é necessário**:
```bash
dotnet new install ./Bmg.Template.Net.Api.<versao>.nupkg
dotnet new <short-name-do-template> -n Bmg.{Servico}   # ex.: -n Bmg.CreditoConsignado
```
> O `<short-name-do-template>` e o `sourceName` (token de contexto, ex.: `ConsigBoilerplate`) estão em `.template.config/template.json`. Confirme-os antes de gerar.

### Ao adicionar artefatos a um serviço já criado
Use sempre **o prefixo da solução atual** (`Bmg.{Servico}.*`). Leia o nome do `.sln` e dos `.csproj` existentes e siga-o — não introduza `ConsigBoilerplate` nem outro nome. Cada novo projeto/classe já nasce correto (nada a renomear).

### Fallback — renomear uma solução já clonada como `ConsigBoilerplate`
Use **somente** se a solução já foi copiada com o nome do template e precisa ser renomeada de uma vez. O token `ConsigBoilerplate` **não deve sobrar** no código final.

**O que renomear (`ConsigBoilerplate` → `{Servico}`)**

| Item | De | Para |
|---|---|---|
| Solution | `Bmg.ConsigBoilerplate.sln` | `Bmg.{Servico}.sln` |
| Projetos (Core) | `Bmg.ConsigBoilerplate.Domain` / `.Application` | `Bmg.{Servico}.Domain` / `.Application` |
| Projetos (Adapters) | `Bmg.ConsigBoilerplate.Database` / `.Api` | `Bmg.{Servico}.Database` / `.Api` |
| Integrações | `Bmg.ConsigBoilerplate.Metabusca` / `.FaceTec` | `Bmg.{Servico}.Metabusca` / `.FaceTec` |
| Testes | `Bmg.ConsigBoilerplate.*.Test` | `Bmg.{Servico}.*.Test` |
| Namespaces | `namespace Bmg.ConsigBoilerplate.*` | `namespace Bmg.{Servico}.*` |
| Classes de DI | `ConsigBoilerplateApiDependency`, `...ApplicationDependency`, `...DatabaseDependency`, `ConsigBoilerplateMemoryDatabase` | `{Servico}ApiDependency`, `{Servico}...` |
| Integração (classe/método) | `ConsigBoilerplate{Nome}Dependency` / `AddConsigBoilerplate{Nome}Module()` | `{Servico}{Nome}Dependency` / `Add{Servico}{Nome}Module()` |
| Chaves de config | `"ConsigBoilerplate.Api:RateLimit:..."` (em `appsettings*` e `Program.cs`) | `"{Servico}.Api:RateLimit:..."` |
| Sample CRUD¹ | `IConsigBoilerplateService` / `ConsigBoilerplateService` / `ConsigBoilerplateController` / `ConsigBoilerplateAppService` | `{Servico}*` (ou já o nome da entidade real) |

¹ As classes do sample (Weather CRUD) serão substituídas pelo seu domínio real; renomeá-las junto mantém tudo compilando até a substituição.

**Procedimento (script — confiável para devs e IA)**
```bash
SERVICO="CreditoConsignado"     # PascalCase → Bmg.CreditoConsignado.*
SIGLA="ccsg"                    # prefixo da aplicação
FISICO="credito-consignado"     # nome físico (kebab-case)

# 1) Conteúdo dos arquivos: ConsigBoilerplate → {Servico}
grep -rlZ --exclude-dir={.git,bin,obj} 'ConsigBoilerplate' . \
  | xargs -0 sed -i "s/ConsigBoilerplate/${SERVICO}/g"

# 2) Renomeie pastas e arquivos (profundidade primeiro)
find . -depth -name '*ConsigBoilerplate*' -not -path './.git/*' | while read -r p; do
  mv "$p" "$(echo "$p" | sed "s/ConsigBoilerplate/${SERVICO}/g")"
done

# 3) Identidade da aplicação (token separado do contexto)
grep -rlZ --exclude-dir={.git,bin,obj} 'WatherForecast' . | xargs -0 sed -i "s/WatherForecast/${SERVICO}/g"
#   Program.cs : ApplicationPrefix "wfcst" → "${SIGLA}" ; ApplicationName "weather-forecast-api" → "${FISICO}"
#   .Api.csproj: <AssemblyName>wfcst-weather-forecast-api</AssemblyName> → "${SIGLA}-${FISICO}"

# 4) Valide
dotnet build && SWAGGER_GENERATION=true dotnet build
```

> ⚠️ `{Servico}` (contexto/namespace) e `{sigla}`/`{servico-fisico}` (identidade física) são **independentes**. Renomear só o namespace sem ajustar a identidade compila, mas publica a aplicação ainda com o nome do template. Ajuste os dois.

---

## Project Structure

```text
Bmg.ConsigBoilerplate/                         # Solution · app: weather-forecast-api · prefixo: wfcst
├── Bmg.ConsigBoilerplate.sln
├── Bmg.Template.Net.Api.10.1.0.nupkg          # Template empacotado (dotnet new)
├── nuget.exe
├── estrutura.txt
├── README.md
├── archetype.md                               # ← este guia
├── .github/
│   ├── copilot-instructions.md                # Regras arquiteturais (fonte normativa)
│   ├── prompts/                               # Prompts de governança de API
│   │   ├── api-contract-governance.prompt.md
│   │   ├── api-design-first.prompt.md
│   │   ├── api-swagger-quality-gate.prompt.md
│   │   └── document-swagger.prompt.md
│   └── skills/
│       └── swagger-generation-skill.md
│
├── Core/                                       # NÚCLEO — ZERO dependência de infraestrutura
│   ├── Domain/
│   │   └── Bmg.ConsigBoilerplate.Domain/
│   │       ├── DatabaseConnection.cs           # Abstração de conexões relacionais (Oracle)
│   │       ├── DatabaseNoSqlConnection.cs      # Abstração de conexões NoSQL
│   │       ├── KafkaCluster.cs                 # Definição de cluster/tópicos Kafka
│   │       ├── Models/
│   │       │   └── v1/WeatherModel.cs          # [Port interno] Modelo de domínio puro
│   │       ├── Services/
│   │       │   └── v1/IConsigBoilerplateService.cs   # [Port de ENTRADA] interface de serviço
│   │       └── Adapters/                        # [Ports de SAÍDA] interfaces p/ infra
│   │           └── Integrations/
│   │               ├── Apis/
│   │               │   ├── Bmg/Metabusca/v1/
│   │               │   │   ├── IMetabuscaApiManager.cs
│   │               │   │   └── ReceitaFederal/ReceitaFederalResponse.cs
│   │               │   └── FaceTec/v1/
│   │               │       ├── IFaceTecApiManager.cs
│   │               │       └── Authentication/AuthenticationRequest.cs · AuthenticationResponse.cs
│   │               └── Queues/
│   │                   └── WeatherConsumerService/WeatherMessage.cs
│   │
│   └── Application/
│       └── Bmg.ConsigBoilerplate.Application/
│           ├── ConsigBoilerplateApplicationDependency.cs   # DI do módulo Application
│           ├── Services/
│           │   └── v1/ConsigBoilerplateService.cs          # Implementa o port de entrada
│           └── Mappings/
│               └── v1/ModelMappingProfile.cs               # AutoMapper Model ↔ Entity
│
├── Adapters/
│   ├── Driving/                                 # ENTRADA (HTTP/REST)
│   │   ├── appsettings.Development.json
│   │   └── Apis/
│   │       └── Bmg.ConsigBoilerplate.Api/
│   │           ├── Program.cs                              # Bootstrap (guarda isSwaggerMode)
│   │           ├── appsettings.json
│   │           ├── ConsigBoilerplateApiDependency.cs       # DI do módulo Api (AppServices)
│   │           ├── ConsigBoilerplateKafkaDependency.cs     # DI do consumidor Kafka
│   │           ├── Controllers/
│   │           │   └── v1/ConsigBoilerplateController.cs
│   │           ├── AppServices/
│   │           │   └── v1/
│   │           │       ├── ConsigBoilerplateAppService.cs
│   │           │       └── Interfaces/IConsigBoilerplateAppService.cs
│   │           ├── Dtos/
│   │           │   └── v1/ConsigBoilerplate/WeatherRequest.cs · WeatherResponse.cs
│   │           ├── Validators/
│   │           │   └── v1/ConsigBoilerplate/WeatherRequestValidator.cs   # FluentValidation
│   │           ├── Mappings/
│   │           │   └── v1/DtoMappingProfile.cs             # AutoMapper DTO ↔ Model
│   │           ├── HealthCheck/Dashboard/bmg.css
│   │           └── Properties/launchSettings.json
│   │
│   └── Driven/                                  # SAÍDA (banco, integrações, filas)
│       ├── Bmg.ConsigBoilerplate.Database/
│       │   ├── ConsigBoilerplateDatabaseDependency.cs
│       │   ├── ConsigBoilerplateMemoryDatabase.cs          # Banco em memória (sample/DEV)
│       │   ├── ConsigBoilerplateNoSqlDatabaseDependency.cs
│       │   ├── Entities/
│       │   │   ├── v1/Weather.cs
│       │   │   └── v1/NoSql/User.cs
│       │   ├── Repositories/
│       │   │   ├── Interfaces/v1/IWeatherRepository.cs
│       │   │   ├── Interfaces/v1/NoSql/IUserRepository.cs
│       │   │   ├── v1/WeatherRepository.cs                 # Dapper + GenericRepository
│       │   │   └── v1/NoSql/UserRepository.cs
│       │   └── UnitOfWork/
│       │       ├── Interfaces/v1/IUnitOfWorkOracle.cs · IUnitOfWorkOracleContext.cs
│       │       └── v1/UnitOfWorkOracle.cs · UnitOfWorkOracleContext.cs
│       └── Integrations/
│           └── Apis/
│               ├── Bmg/Bmg.ConsigBoilerplate.Metabusca/
│               │   ├── ConsigBoilerplateMetabuscaDependency.cs
│               │   └── v1/MetabuscaApiManager.cs           # Implementa IMetabuscaApiManager
│               └── Bmg.ConsigBoilerplate.FaceTec/
│                   ├── ConsigBoilerplateFaceTecDependency.cs
│                   └── v1/FaceTecApiManager.cs             # Implementa IFaceTecApiManager
│
└── Tests/                                       # Espelha a estrutura de produção (xUnit)
    ├── Core/Application/Bmg.ConsigBoilerplate.Application.Test/
    │   └── Services/v1/ConsigBoilerplateServiceTest.cs
    └── Adapters/
        ├── Driving/Apis/Bmg.ConsigBoilerplate.Api.Test/
        │   └── v1/ConsigBoilerplateControllerTest.cs
        └── Driven/Integrations/Apis/
            ├── Bmg/Bmg.ConsigBoilerplate.Metabusca.Test/v1/MetabuscaApiManagerTest.cs
            └── Bmg.ConsigBoilerplate.FaceTec.Test/v1/FaceTecApiManagerTest.cs
```

---

## Arquitetura em Camadas (Hexagonal · Ports & Adapters)

```
Cliente HTTP / Gateway (Sensedia)
    ↕
Adapters/Driving  →  Api (Controllers, AppServices, DTOs, Validators, Program.cs)
    ↕
Core/Application  →  Services (regras de negócio, orquestração, UnitOfWork, Notifier)
    ↕
Core/Domain       →  Models, Services (ports de entrada), Adapters (ports de saída)
    ↕
Adapters/Driven   →  Database (Dapper/UnitOfWork), Integrations (Bmg.Api.Client), Queues (Kafka)
```

**Princípio fundamental**: camadas internas **nunca** conhecem camadas externas. O `Domain` não depende de nada além de `Bmg.Project.Utils` (`arqc-project-utils`). A `Application` fala com a infra **somente** através de interfaces (ports) declaradas no `Domain`.

### Matriz de dependências (NUNCA violar)

| Camada | Pode depender de | NUNCA depender de |
|---|---|---|
| `Domain` | apenas `Bmg.Project.Utils` | `Application`, `Database`, `Api`, EF, Dapper |
| `Application` | `Domain` | `Database` direto (sempre via port/interface), `Api` |
| `Database` (Driven) | `Domain` (interfaces/models) | `Application`, `Api` |
| `Integrations` (Driven) | `Domain` (interfaces) | `Application`, `Api` |
| `Api` (Driving) | `Domain` (interfaces), `Application` | `Database` / `Integrations` direto |

---

## Camada 1: `Domain` — Núcleo do negócio

**Responsabilidades**: Modelos de domínio puros, **ports de entrada** (interfaces de serviço) e **ports de saída** (interfaces para banco, integrações e filas). **Zero dependência de infraestrutura.**

**Exemplo — Modelo de domínio** (`Core/Domain/.../Models/v1/WeatherModel.cs`):
```csharp
namespace Bmg.ConsigBoilerplate.Domain.Models.v1
{
    // record → imutável; usado com "with" para cópias seguras na Application
    public record WeatherModel
    {
        public long Id { get; init; }
        public DateOnly Date { get; init; }
        public int TemperatureC { get; init; }
        public string? Summary { get; init; }
    }
}
```

**Exemplo — Port de ENTRADA** (`Core/Domain/.../Services/v1/IConsigBoilerplateService.cs`):
```csharp
using Bmg.Project.Utils.Data;          // PaginatedData<T>
using Bmg.Project.Utils.Interfaces;    // IBmgServiceBase
using Bmg.ConsigBoilerplate.Domain.Models.v1;
using Microsoft.AspNetCore.JsonPatch;

namespace Bmg.ConsigBoilerplate.Domain.Services.v1
{
    public interface IConsigBoilerplateService : IBmgServiceBase
    {
        Task<IEnumerable<WeatherModel>> GetWeathersAsync(CancellationToken cancellationToken);
        Task<PaginatedData<WeatherModel>> GetWeathersPaginatedAsync(int pageSize, int pageNumber, CancellationToken cancellationToken);
        Task<WeatherModel> GetWeatherAsync(long id, CancellationToken cancellationToken);
        Task<WeatherModel> CreateWeatherAsync(WeatherModel weather, CancellationToken cancellationToken);
        Task<bool> PatchWeatherAsync(long id, JsonPatchDocument<WeatherModel> weatherPatch, CancellationToken cancellationToken);
        Task<bool> UpdateWeatherAsync(WeatherModel weather, CancellationToken cancellationToken);
        Task<bool> DeleteWeatherAsync(long id, CancellationToken cancellationToken);
    }
}
```

**Exemplo — Port de SAÍDA (integração externa)** (`Core/Domain/.../Adapters/Integrations/Apis/.../FaceTec/v1/IFaceTecApiManager.cs`):
```csharp
namespace Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.FaceTec.v1
{
    public interface IFaceTecApiManager
    {
        void SetCancellationToken(CancellationToken cancellationToken);
        Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request);
    }
}
```

> O `Domain` também declara `DatabaseConnection` / `DatabaseNoSqlConnection` (enum de conexões) e `KafkaCluster`. Repositórios e consumidores na camada Driven **implementam** os ports definidos aqui.

---

## Camada 2: `Application` — Regras de negócio e orquestração

**Responsabilidades**: Implementar os ports de entrada do `Domain`, orquestrar repositórios (via `UnitOfWork`) e integrações (via ports de saída), abrir transações e emitir **notificações** de negócio. Herda de `BmgServiceBase` e é decorada com `[BmgDynatraceTrace]`.

**Exemplo — Domain Service** (`Core/Application/.../Services/v1/ConsigBoilerplateService.cs`):
```csharp
using Bmg.Project.Utils.Base;          // BmgServiceBase (Mapper, Notifier)
using Bmg.Project.Utils.Data;          // PaginatedData<T>
using Bmg.ConsigBoilerplate.Database.Entities.v1;
using Bmg.ConsigBoilerplate.Database.UnitOfWork.Interfaces.v1;
using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.FaceTec.v1;
using Bmg.ConsigBoilerplate.Domain.Models.v1;
using Bmg.ConsigBoilerplate.Domain.Services.v1;
using Microsoft.AspNetCore.JsonPatch;
using System.Transactions;

namespace Bmg.ConsigBoilerplate.Application.Services.v1
{
    [BmgDynatraceTrace]
    public class ConsigBoilerplateService : BmgServiceBase, IConsigBoilerplateService
    {
        private readonly IUnitOfWorkOracle _unitOfWork;      // port de saída (banco)
        private readonly IFaceTecApiManager _faceTecApiManager; // port de saída (integração)

        public ConsigBoilerplateService(IUnitOfWorkOracle unitOfWork, IFaceTecApiManager faceTecApiManager)
        {
            _unitOfWork = unitOfWork;
            _faceTecApiManager = faceTecApiManager;
        }

        public async Task<WeatherModel> GetWeatherAsync(long id, CancellationToken ct)
        {
            var result = await _unitOfWork.Weathers.SelectAsync(ct, id);
            return Mapper.Map<WeatherModel>(result);
        }

        public async Task<PaginatedData<WeatherModel>> GetWeathersPaginatedAsync(int pageSize, int pageNumber, CancellationToken ct)
        {
            var result = await _unitOfWork.Weathers.SelectPaginationAsync(pageSize, pageNumber, ct);
            return Mapper.Map<PaginatedData<WeatherModel>>(result);
        }

        public async Task<WeatherModel> CreateWeatherAsync(WeatherModel weather, CancellationToken ct)
        {
            // Transação: todos os repositórios usados dentro do escopo participam do mesmo commit/rollback
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // Exemplo de NOTIFICAÇÃO de negócio (evita exceptions como fluxo de retorno → 422)
            if (weather.Summary?.Equals("NotifyTest", StringComparison.OrdinalIgnoreCase) == true)
            {
                await Notifier.NotifyAsync(nameof(Weather), "Summary cannot be NotifyTest");
                return null;
            }

            var result = weather with { Id = weather.Id };

            transaction.Complete(); // sem Complete() → rollback automático ao sair do escopo
            return result;
        }

        public async Task<bool> UpdateWeatherAsync(WeatherModel weather, CancellationToken ct)
        {
            var current = await GetWeatherAsync(weather.Id, ct);
            if (current == null) return false;
            return await _unitOfWork.Weathers.UpdateAsync(Mapper.Map<Weather>(weather), ct);
        }
        // PatchWeatherAsync / DeleteWeatherAsync seguem o mesmo padrão (busca → valida → persiste)
    }
}
```

**Padrões a observar:**
- **`UnitOfWork`** expõe os repositórios (`_unitOfWork.Weathers`) e garante transação compartilhada.
- **`Notifier.NotifyAsync(...)`** acumula mensagens de negócio que o Controller traduz em **HTTP 422** — nunca lance `Exception` para validação de negócio.
- **`TransactionScope`** com `TransactionScopeAsyncFlowOption.Enabled` envolve múltiplas escritas; `Complete()` confirma, ausência dele faz rollback.
- O `ModelMappingProfile` (em `Application/Mappings/v1/`) configura o AutoMapper `WeatherModel ↔ Weather` (entity).

---

## Camada 3: `Driven` — Adaptadores de saída (infraestrutura)

Implementações concretas dos ports de saída definidos no `Domain`.

### 3a. Repositório relacional (Dapper + `GenericRepository`)

**Interface** (`Adapters/Driven/.../Repositories/Interfaces/v1/IWeatherRepository.cs`):
```csharp
using Bmg.Connection.Manager.Data;     // IGenericRepository
using Bmg.ConsigBoilerplate.Database.Entities.v1;
using Bmg.ConsigBoilerplate.Domain;    // DatabaseConnection

public interface IWeatherRepository : IGenericRepository<DatabaseConnection, Weather> { }
```

**Implementação** (`Adapters/Driven/.../Repositories/v1/WeatherRepository.cs`):
```csharp
using Bmg.Connection.Manager.Data;     // GenericRepository, SqlBuilder
using Bmg.ConsigBoilerplate.Database.Entities.v1;
using Bmg.ConsigBoilerplate.Database.Repositories.Interfaces.v1;
using Bmg.ConsigBoilerplate.Domain;
using Dapper;

namespace Bmg.ConsigBoilerplate.Database.Repositories.v1
{
    public class WeatherRepository : GenericRepository<DatabaseConnection, Weather>, IWeatherRepository
    {
        public WeatherRepository(IServiceProvider provider) : base(DatabaseConnection.Oracle, provider) { }

        public override async Task<Weather> SelectAsync(CancellationToken ct, params object[] ids)
        {
            var builder = new SqlBuilder();
            var template = builder.AddTemplate("SELECT * FROM teste.tbl_wth /**where**/");
            builder.Where("wth_id = @Id");

            return await Connection.QueryFirstOrDefaultAsync<Weather>(template.RawSql, new { Id = ids[0] });
        }
    }
}
```

**Regras do repositório:**
- Sempre **Dapper** via `GenericRepository<TConnection, TEntity>` — **Entity Framework não é o padrão** (use só com justificativa).
- `SqlBuilder` + `/**where**/` para montar SQL parametrizado (evita SQL injection).
- `Connection` e os métodos base (`QueryAsync`, `QueryFirstOrDefaultAsync`, `UpdateAsync`, `DeleteAsync`, `SelectPaginationAsync`) vêm de `Bmg.Connection.Manager`.
- O `UnitOfWorkOracle` agrega os repositórios e é injetado na `Application`.

### 3b. Integração externa (`Bmg.Api.Client`)

Cada integração externa é um **projeto próprio** sob `Adapters/Driven/Integrations/Apis/` que implementa um port de `Domain/Adapters/Integrations`. A classe herda `ApiBase` e injeta **`IBmgApiClient`** (de `Bmg.Api.Client.Base`) — que encapsula Flurl, propaga `x-bmg-id`, aplica rate limit e logging. Padrão real (`MetabuscaApiManager`):

```csharp
using Bmg.Api.Client;
using Bmg.Api.Client.Base;        // IBmgApiClient, ApiBase
using Flurl;

public class MetabuscaApiManager : ApiBase, IMetabuscaApiManager
{
    public const string EndPointCliente = "api/v1/cliente";

    public MetabuscaApiManager(IBmgApiClient apiClient, ILogger<MetabuscaApiManager> logger, IConfiguration configuration)
        : base(configuration.GetValue<string>("Apis:Metabusca:PathUrl"), apiClient, logger) { }

    public async Task<ReceitaFederalResponse> ValidaCpfAsync(string cpf)
    {
        try
        {
            return await ApiClient
                .Url(Url.Combine(EndPointCliente, cpf, "validar"))
                .WithBmgHeaders<ReceitaFederalResponse>(HeaderType.Response)
                .PostAsync(cancellationToken: CancellationToken)
                .ReceiveJson<ReceitaFederalResponse>();
        }
        catch (FlurlHttpException ex)
        {
            await LogApiExceptionAsync(ex);   // log estruturado herdado de ApiBase
            throw;
        }
    }
}
```

> O passo a passo para criar uma nova integração (projeto, Facade, dependência, registro) está em **"Integrações externas — projeto por integração"**.

### 3c. Fila / Kafka (opcional)

`Domain/Adapters/Integrations/Queues/WeatherConsumerService/WeatherMessage.cs` define o contrato da mensagem; o consumidor é registrado por `ConsigBoilerplateKafkaDependency` e ativado em `Program.cs` apenas quando o serviço usa Kafka.

### 3d. UnitOfWork — `UnitOfWorkOracle` e `UnitOfWorkOracleContext`

O `UnitOfWorkOracle` é o agregador de repositórios injetado na `Application`. Expõe cada repositório como propriedade resolvida sob demanda via `IServiceProvider` (a interface `IUnitOfWorkOracle` declara a mesma assinatura):

```csharp
public class UnitOfWorkOracle : IUnitOfWorkOracle
{
    private readonly IServiceProvider _provider;
    public UnitOfWorkOracle(IServiceProvider provider) => _provider = provider;

    public IWeatherRepository Weathers => _provider.GetRequiredService<IWeatherRepository>();
    // Uma propriedade por repositório novo
}
```

O `UnitOfWorkOracleContext` é um `DbContext` (EF Core) com um `DbSet<>` por entidade. **Atenção**: no template ele está marcado `[Obsolete]` — *"o uso de EF Core não está liberado no Banco Bmg... por hora este UnitOfWork não deve ser utilizado"*. Existe apenas para facilitar uma futura transição; a persistência real é **Dapper via `GenericRepository`**.

```csharp
[Obsolete("EF Core não liberado no Banco Bmg — não utilizar por hora")]
public class UnitOfWorkOracleContext : DbContext, IUnitOfWorkOracleContext
{
    public UnitOfWorkOracleContext(DbContextOptions<UnitOfWorkOracleContext> options) : base(options) { }

    public DbSet<Weather> Weathers { get; set; }   // um DbSet por entidade
}
```

---

## Camada 4: `Driving` — Adaptadores de entrada (API)

### 4a. Controller — só recebe a requisição, delega e trata HTTP

`ConsigBoilerplateController` herda `BmgControllerBase<IConsigBoilerplateAppService>` e expõe o CRUD completo. **Nenhuma regra de negócio aqui.**

```csharp
[ApiController]
[ApiVersion("1")]
[Route("v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(IEnumerable<BmgNotification>), StatusCodes.Status422UnprocessableEntity)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ConsigBoilerplateController : BmgControllerBase<IConsigBoilerplateAppService>
{
    [HttpGet("{id}", Name = nameof(GetAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<WeatherResponse>> GetAsync(long id, CancellationToken ct)
    {
        var result = await AppService.GetAsync(id, ct);
        if (HasNotifications()) return Notifications();   // → 422 com a lista de notificações
        return result != null ? Ok(result) : NoContent();
    }

    [HttpGet("{pageSize}/{currentPage}")]
    public async Task<ActionResult<IEnumerable<WeatherResponse>>> GetPaginatedAsync(int pageSize, int currentPage, CancellationToken ct)
    {
        var result = await AppService.GetPaginatedAsync(pageSize, currentPage, ct);
        if (HasNotifications()) return Notifications();
        return OkPaginated(result);                       // → 206 Partial Content
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<WeatherResponse>> PostAsync(WeatherRequest weather, CancellationToken ct)
    {
        var result = await AppService.PostAsync(weather, ct);
        if (HasNotifications()) return Notifications();
        return CreatedAtRoute(string.Empty, new { id = result.Id }, result);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchAsync(long id, JsonPatchDocument<WeatherRequest> weather, CancellationToken ct)
    {
        var ok = await AppService.PatchAsync(id, weather, ct);
        if (HasNotifications()) return Notifications();
        return ok ? NoContent() : BadRequest();
    }
    // PutAsync / DeleteAsync seguem o mesmo padrão
}
```

**Convenções do Controller:**
- `HasNotifications()` + `Notifications()` → traduz notificações de negócio em **422**.
- `OkPaginated(...)` → **206**; `CreatedAtRoute(...)` → **201**; `NoContent()` → **204**.
- `PATCH` usa `JsonPatchDocument<WeatherRequest>` (operação + campo).
- Versão na rota (`v{version:apiVersion}`) e em todas as subpastas.

### 4b. AppService — único responsável pelo mapeamento DTO ↔ Model

`ConsigBoilerplateAppService` herda `BmgAppServiceBase<IConsigBoilerplateService>` e é a **única** classe que converte DTO ↔ Domain Model:

```csharp
[BmgDynatraceTrace]
public class ConsigBoilerplateAppService : BmgAppServiceBase<IConsigBoilerplateService>, IConsigBoilerplateAppService
{
    public async Task<WeatherResponse> PostAsync(WeatherRequest request, CancellationToken ct)
    {
        var model  = Mapper.Map<WeatherModel>(request);          // DTO → Model
        var result = await Service.CreateWeatherAsync(model, ct); // chama o Domain
        return Mapper.Map<WeatherResponse>(result);              // Model → DTO
    }

    public async Task<bool> PatchAsync(long id, JsonPatchDocument<WeatherRequest> request, CancellationToken ct)
    {
        var patch = Mapper.Map<JsonPatchDocument<WeatherModel>>(request);
        return await Service.PatchWeatherAsync(id, patch, ct);
    }
}
```

### 4c. DTOs + Validators (FluentValidation)

```csharp
// Dtos/v1/ConsigBoilerplate/WeatherRequest.cs
public record WeatherRequest
{
    public DateOnly Date { get; init; }
    public int TemperatureC { get; init; }
    public string? Summary { get; init; }
}

// Validators/v1/ConsigBoilerplate/WeatherRequestValidator.cs
public class WeatherRequestValidator : AbstractValidator<WeatherRequest>
{
    public WeatherRequestValidator()
    {
        RuleFor(x => x.TemperatureC).InclusiveBetween(-60, 60);
        RuleFor(x => x.Summary).MaximumLength(120);
    }
}
```

> DTOs (`Driving`) **nunca** são expostos ao `Domain`. O `DtoMappingProfile` (`Mappings/v1/`) configura DTO ↔ Model. Validação de **contrato** (campo obrigatório, tipo) → **400**; validação de **negócio** (no `Application` via `Notifier`) → **422**.

---

## Program.cs e Bootstrap — a guarda `isSwaggerMode`

O `Program.cs` é o ponto de composição. A regra mais importante: **todo módulo que depende de infraestrutura real** (banco, Kafka, CNFG, APIs externas, auth) **deve ser registrado dentro de `if (!isSwaggerMode)`**, para que a geração estática do contrato OpenAPI no build não tente subir infra indisponível.

```csharp
public class Program
{
    private const string ApplicationPrefix = "wfcst";            // sigla da aplicação
    private const string ApplicationName   = "weather-forecast-api"; // nome sem a sigla

    public static async Task<int> Main(string[] args)
    {
        BmgProjectUtils.SetProjectExecutionFolder();
        var builder = WebApplication.CreateBuilder(args);

        // QUALITY GATEWAY — modo de geração estática do Swagger (sem acoplar infra no build)
        var isSwaggerMode = string.Equals(
            Environment.GetEnvironmentVariable("SWAGGER_GENERATION"), "true",
            StringComparison.OrdinalIgnoreCase);

        builder.AddBmgLoggingInternal();

        // MÓDULOS CORE — sempre registrados (necessários para o CLI descobrir endpoints)
        builder.Services.AddAutoMapper(
            typeof(Application.Mappings.v1.ModelMappingProfile),
            typeof(Mappings.v1.DtoMappingProfile));
        builder.Services.AddWatherForecastApiModule();       // Controllers, AppServices, interfaces
        builder.Services.AddBmgMemoryCacheManager();

        // MÓDULOS DE INFRA — ignorados na geração do Swagger
        if (!isSwaggerMode)
        {
            builder.Configuration.AddBmgParameterManagerSetup(ApplicationPrefix, ApplicationName)
                .AddBmgParametersSecrets()
                .AddBmgParametersApplication()
                .AddBmgParametersBrokers();

            builder.Services.AddWatherForecastDatabaseModule(builder.Configuration);
            // builder.Services.AddWatherForecastNoSqlDatabaseModule(builder.Configuration); // se NoSQL
            builder.Services.AddWatherForecastApplicationModule();
            builder.Services.AddBmgAuth(ApplicationPrefix, ApplicationName, builder.Configuration);
            builder.AddBmgApiClient(builder.Configuration.GetValue<int>("ConsigBoilerplate.Api:ApiConsumptionTimeoutMs"));
            builder.Services.AddConsigBoilerplateMetabuscaModule();
            builder.Services.AddConsigBoilerplateFaceTecModule();
            // builder.Services.AddConsigBoilerplateKafkaModule(builder.Configuration); // se Kafka
        }

        // Configura Swagger/OpenAPI, versioning, rate limit, /healthz, Kestrel, controllers
        var app = builder.AddBmgApiProjectDependencies(
            ApplicationPrefix, ApplicationName, typeof(Program),
            "Weather Forecast API", "An ASP.NET Core Web API for managing Weather Forecast items",
            /* terms, contact, license, deprecationMsg ... */
            builder.Configuration.GetValue<int>("ConsigBoilerplate.Api:RateLimit:MaxRequests"),
            builder.Configuration.GetValue<TimeSpan>("ConsigBoilerplate.Api:RateLimit:MaxRequestsWindow"),
            builder.Configuration.GetValue<int>("ConsigBoilerplate.Api:MaxPagination"),
            true);

        ConsigBoilerplateMemoryDatabase.AddInMemoryDatabase(app.Services); // remover ao plugar banco real

        app.UseBmgLoggingInternal(app.Configuration);
        if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

        if (!isSwaggerMode)
        {
            app.UseBmgApiClient();
            app.UseBmgAuth();
        }

        await app.RunAsync();
        return 0;
    }
}
```

**O que NÃO guardar** (precisa estar visível para o Swagger): `AddAutoMapper`, `Add...ApiModule()`, `Add...ApplicationModule()`, `AddBmgApiProjectDependencies`.

---

## Registro de dependências (DI por camada)

Cada camada tem **uma classe estática de dependência** que expõe um método de extensão `Add...Module(...)`, chamado no `Program.cs`. Ao criar um novo AppService / Service / Repository, **é obrigatório EDITAR a classe correspondente e adicionar o registro** — os blocos abaixo não são exemplos opcionais. Sem o registro, a injeção de dependência falha em runtime (`Unable to resolve service...`) mesmo com o código compilando.

**API — `Adapters/Driving/Apis/.../ConsigBoilerplateApiDependency.cs`** (`AddWatherForecastApiModule`): registra os **AppServices**.
```csharp
public static void AddWatherForecastApiModule(this IServiceCollection services)
{
    services.AddScoped<IConsigBoilerplateAppService, ConsigBoilerplateAppService>();
    services.AddScoped<IPropostaAppService, PropostaAppService>();   // ← novo AppService
}
```

**Application — `Core/Application/.../ConsigBoilerplateApplicationDependency.cs`** (`AddWatherForecastApplicationModule`): registra os **Domain Services**.
```csharp
public static void AddWatherForecastApplicationModule(this IServiceCollection services)
{
    services.AddScoped<IConsigBoilerplateService, ConsigBoilerplateService>();
    services.AddScoped<IUsuarioService, UsuarioService>();          // ← novo Service
}
```

**Database — `Adapters/Driven/.../ConsigBoilerplateDatabaseDependency.cs`** (`AddWatherForecastDatabaseModule`): registra o `UnitOfWork`, as conexões e os **repositórios** (via `AddBmgScopedRepository`).
```csharp
public static void AddWatherForecastDatabaseModule(this IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<IUnitOfWorkOracle, UnitOfWorkOracle>();

    services.AddBmgConnectionManager<DatabaseConnection>()
        .AddConnection<IUnitOfWorkOracleContext, UnitOfWorkOracleContext>(
            DatabaseConnection.Oracle,                              // chave ÚNICA por conexão
            (provider, options) => options.UseInMemoryDatabase("teste_api"));
            // PROD: options.UseOracle(configuration.GetConnectionString("ExadataConnection"))

    services.AddBmgScopedRepository<IWeatherRepository, WeatherRepository>();
    services.AddBmgScopedRepository<IUsuarioRepository, UsuarioRepository>();  // ← novo repositório
}
```
> Use `AddBmgScopedRepository<IInterface, Implementação>()` — **interface + implementação** (não a interface duas vezes).

---

## Integrações externas — projeto por integração

Toda integração externa (Metabusca, Consig, FaceTec, PN, ...) é um **projeto .NET próprio** em `Adapters/Driven/Integrations/Apis/` que implementa um port declarado no `Domain`. **Criar apenas a classe `*Dependency.cs` ou `*Facade.cs` NÃO cria o projeto** — execute o checklist inteiro.

> 📌 Nos exemplos abaixo, `ConsigBoilerplate` é o contexto do template-base. **Crie já com o prefixo da sua solução** (`Bmg.{Servico}.*`, ex.: `Bmg.{Servico}.PN`, `{Servico}PNDependency`, `Add{Servico}PNModule()`) — não renomeie depois.

**Convenções**
- **Port (Domain)**: integrações **BMG** → `...Domain.Adapters.Integrations.Apis.Bmg.{Nome}.v{n}` (ex.: `...Apis.Bmg.Metabusca.v1.IMetabuscaApiManager`); **terceiros/parceiros** → `...Domain.Adapters.Integrations.Apis.{Nome}.v{n}` (ex.: `...Apis.FaceTec.v1.IFaceTecApiManager`, **sem** o segmento `Bmg`).
- **Pasta do projeto**: BMG → `Integrations/Apis/Bmg/Bmg.ConsigBoilerplate.{Nome}/`; parceiros → `Integrations/Apis/{Nome}/Bmg.ConsigBoilerplate.{Nome}/`.
- **Implementação** (`v{n}/{Nome}ApiManager.cs` ou `{Nome}Facade.cs`): herda `ApiBase`, injeta **`IBmgApiClient`** (`using Bmg.Api.Client;` + `using Bmg.Api.Client.Base;`) — nunca um `IApiClient` genérico.

### Checklist de arquivos — nova integração

**CRIAR**

1. **`Bmg.ConsigBoilerplate.{Nome}.csproj`** (sem este arquivo NÃO existe projeto):
```xml
<!-- Adapters/Driven/Integrations/Apis/{Nome}/Bmg.ConsigBoilerplate.{Nome}/Bmg.ConsigBoilerplate.{Nome}.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>annotations</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Bmg.Api.Client" Version="10.*" />
    <PackageReference Include="Bmg.Logging.Internal" Version="10.*" />
  </ItemGroup>
  <!-- A referência ao Domain (port I{Nome}ApiManager) é adicionada via CLI no passo 2 -->
</Project>
```
2. **Adicionar à solution e referenciar o `Domain`** (use o CLI — ele calcula o caminho relativo correto, que varia conforme o aninhamento de pastas):
```bash
PROJ=Adapters/Driven/Integrations/Apis/{Nome}/Bmg.ConsigBoilerplate.{Nome}/Bmg.ConsigBoilerplate.{Nome}.csproj
dotnet sln Bmg.ConsigBoilerplate.sln add $PROJ
dotnet add $PROJ reference Core/Domain/Bmg.ConsigBoilerplate.Domain/Bmg.ConsigBoilerplate.Domain.csproj
```
3. **`v{n}/{Nome}ApiManager.cs`** (ou `{Nome}Facade.cs`) — herda `ApiBase`, injeta `IBmgApiClient` (ver seção 3b).
4. **`I{Nome}ApiManager.cs`** no `Domain` (port), se ainda não existir.
5. **`ConsigBoilerplate{Nome}Dependency.cs`** — classe de dependência:
```csharp
public static class ConsigBoilerplatePNDependency
{
    public static void AddConsigBoilerplatePNModule(this IServiceCollection services)
        => services.AddScoped<IPNApiManager, PNFacade>();   // port → implementação
}
```

**EDITAR (obrigatório — não pular)**

6. **`Bmg.ConsigBoilerplate.Api.csproj`** — adicionar o `<ProjectReference>` do novo projeto:
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\Driven\Integrations\Apis\Bmg\Bmg.ConsigBoilerplate.Metabusca\Bmg.ConsigBoilerplate.Metabusca.csproj" />
  <ProjectReference Include="..\..\..\Driven\Integrations\Apis\Bmg.ConsigBoilerplate.FaceTec\Bmg.ConsigBoilerplate.FaceTec.csproj" />
  <ProjectReference Include="..\..\..\Driven\Integrations\Apis\PN\Bmg.ConsigBoilerplate.PN\Bmg.ConsigBoilerplate.PN.csproj" />          <!-- novo -->
  <ProjectReference Include="..\..\..\Driven\Integrations\Apis\Consig\Bmg.ConsigBoilerplate.Consig\Bmg.ConsigBoilerplate.Consig.csproj" /> <!-- novo -->
</ItemGroup>
```
   Ou via CLI: `dotnet add Adapters/Driving/Apis/Bmg.ConsigBoilerplate.Api/Bmg.ConsigBoilerplate.Api.csproj reference $PROJ`
7. **`Program.cs`** — registrar o módulo dentro de `if (!isSwaggerMode)`:
```csharp
builder.Services.AddConsigBoilerplatePNModule();
builder.Services.AddConsigBoilerplateConsigModule();
```

> ✅ **Concluído quando** `dotnet build` compila. Se `AddConsigBoilerplate{Nome}Module()` não for encontrado → faltou o passo 6 (ProjectReference no `.Api`). Se o build não acha o projeto → faltou o passo 2 (`dotnet sln add`).

---

## Geração de Swagger (API First) — Quality Gate

O contrato OpenAPI é gerado **automaticamente no build** quando `SWAGGER_GENERATION=true`, vindo da `Bmg.Project.Utils` via `buildTransitive` (sem configuração local de geração no `.csproj`).

**Saída (ordem de precedência):**
1. `$(BUILD_ARTIFACTSTAGINGDIRECTORY)/swagger-specs` (pipeline)
2. `$(SolutionDir)/swagger-specs` (build via `.sln`)
3. `$(MSBuildProjectDirectory)/swagger-specs` (build via `.csproj`)

**Arquivos esperados:** `swagger-v*.json` (por versão) e `swagger.json` (alias da maior versão).

```bash
# Conferir o contrato localmente, antes do PR:
dotnet tool restore
SWAGGER_GENERATION=true dotnet build
# → swagger-specs/swagger-v1.json e swagger-specs/swagger.json
```

Na pipeline, a pasta `swagger-specs` é publicada como artefato e enviada ao **Sensedia Adaptive Governance**. `info.version` no OpenAPI carrega **apenas o MAJOR** (`1`, `2`, ...).

---

## Bibliotecas corporativas — catálogo

### 🔴 Obrigatórias — toda API deve usar

| Lib (`arqc-*`) | Pacote NuGet | Papel |
|---|---|---|
| `arqc-project-utils` | `Bmg.Project.Utils` | `AddBmgApiProjectDependencies`, classes base (`BmgControllerBase`, `BmgAppServiceBase`, `BmgServiceBase`), `PaginatedData<T>`, rota/rate limit/`healthz`/Swagger |
| `arqc-api-client` | `Bmg.Api.Client` | HTTP/SOAP para APIs internas (Flurl), `x-bmg-id` via `BmgTraceLogHandler` |
| `arqc-auth` | `Bmg.Auth` | Autenticação/autorização JWT corporativa (Entra ID) |
| `arqc-connection-manager` | `Bmg.Connection.Manager` | `GenericRepository` (Dapper), `IUnitOfWork`, `DatabaseConnection` |
| `arqc-parameters-manager` | `Bmg.Parameter.Manager` | Configuração dinâmica via CNFG (segredos, flags, timeouts) |
| `arqc-logging-internal` | `Bmg.Logging.Internal` | Logs estruturados + `[BmgDynatraceTrace]` |

### 🟡 Recomendadas quando o serviço usa a tecnologia

`arqc-kafka` (`Bmg.Kafka`), `arqc-nosqlconnection-manager` (`Bmg.NoSqlConnection.Manager`, **obrigatória** com NoSQL), `arqc-cache-manager` (`Bmg.Cache.Manager`), `arqc-storage-manager` (S3), `arqc-notification-manager` (ADCN), `arqc-bind-converter` (docx/HTML→PDF), `arqc-call-orchestrator`, `arqc-file-functions-lib`, `arqc-crypto`.

### 🔵 Exceção de tecnologia

`arqc-process-dlq`, `arqc-queue-service` (SQS), `arqc-rabbitmq`.

> **Proibidos**: `MediatR` (ADR-003) e `ErrorOr` (ADR-004). **Acesso a dados**: prefira **Dapper** via `GenericRepository`.

---

## Desenvolvimento de nova feature — Processo em 7 etapas

Para uma entidade nova chamada **`Contract`** (contrato), na versão `v1`:

#### Etapa 1 — Domain: modelo + port de entrada
```csharp
// Core/Domain/.../Models/v1/ContractModel.cs
public record ContractModel { public long Id { get; init; } public decimal Amount { get; init; } public string Status { get; init; } = "open"; }

// Core/Domain/.../Services/v1/IContractService.cs
public interface IContractService : IBmgServiceBase
{
    Task<ContractModel> GetAsync(long id, CancellationToken ct);
    Task<ContractModel> CreateAsync(ContractModel contract, CancellationToken ct);
}
```

#### Etapa 2 — Driven: port de saída + repositório
```csharp
// Adapters/Driven/.../Repositories/Interfaces/v1/IContractRepository.cs
public interface IContractRepository : IGenericRepository<DatabaseConnection, Contract> { }

// Adapters/Driven/.../Repositories/v1/ContractRepository.cs
public class ContractRepository : GenericRepository<DatabaseConnection, Contract>, IContractRepository
{
    public ContractRepository(IServiceProvider provider) : base(DatabaseConnection.Oracle, provider) { }
    // override SelectAsync com SqlBuilder, conforme WeatherRepository
}
// Registrar o repositório no UnitOfWorkOracle (Contracts) e no ...DatabaseDependency.
```

#### Etapa 3 — Application: implementação do service
```csharp
// Core/Application/.../Services/v1/ContractService.cs
[BmgDynatraceTrace]
public class ContractService : BmgServiceBase, IContractService
{
    private readonly IUnitOfWorkOracle _uow;
    public ContractService(IUnitOfWorkOracle uow) => _uow = uow;

    public async Task<ContractModel> GetAsync(long id, CancellationToken ct)
        => Mapper.Map<ContractModel>(await _uow.Contracts.SelectAsync(ct, id));

    public async Task<ContractModel> CreateAsync(ContractModel contract, CancellationToken ct)
    {
        using var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        if (contract.Amount <= 0) { await Notifier.NotifyAsync(nameof(Contract), "Amount must be positive"); return null; }
        var created = await _uow.Contracts.InsertAsync(Mapper.Map<Contract>(contract), ct);
        tx.Complete();
        return Mapper.Map<ContractModel>(created);
    }
}
```

#### Etapa 4 — Api: DTOs + Validator + Mappings
```csharp
// Dtos/v1/Contract/{ContractRequest,ContractResponse}.cs  → records de entrada/saída
// Validators/v1/Contract/ContractRequestValidator.cs       → RuleFor(x => x.Amount).GreaterThan(0);
// Mappings: ModelMappingProfile (Model↔Entity) e DtoMappingProfile (DTO↔Model)
```

#### Etapa 5 — Api: AppService (interface + implementação)
```csharp
// AppServices/v1/Interfaces/IContractAppService.cs
public interface IContractAppService { Task<ContractResponse> GetAsync(long id, CancellationToken ct); Task<ContractResponse> PostAsync(ContractRequest req, CancellationToken ct); }

// AppServices/v1/ContractAppService.cs
[BmgDynatraceTrace]
public class ContractAppService : BmgAppServiceBase<IContractService>, IContractAppService
{
    public async Task<ContractResponse> PostAsync(ContractRequest req, CancellationToken ct)
        => Mapper.Map<ContractResponse>(await Service.CreateAsync(Mapper.Map<ContractModel>(req), ct));
    public async Task<ContractResponse> GetAsync(long id, CancellationToken ct)
        => Mapper.Map<ContractResponse>(await Service.GetAsync(id, ct));
}
```

#### Etapa 6 — Api: Controller
```csharp
// Controllers/v1/ContractController.cs
[ApiController, ApiVersion("1"), Route("v{version:apiVersion}/[controller]")]
public class ContractController : BmgControllerBase<IContractAppService>
{
    [HttpPost]
    public async Task<ActionResult<ContractResponse>> PostAsync(ContractRequest req, CancellationToken ct)
    {
        var result = await AppService.PostAsync(req, ct);
        if (HasNotifications()) return Notifications();
        return CreatedAtRoute(string.Empty, new { id = result.Id }, result);
    }
}
```

#### Etapa 7 — EDITAR os arquivos de registro (DI) — OBRIGATÓRIO
Estas são **edições em arquivos existentes**, não classes novas. **Todas são obrigatórias** — criar `ContractService`/`ContractRepository`/etc. sem fazer estas edições faz a injeção falhar (em runtime ou no build):

```csharp
// EDITAR  Core/Application/.../ConsigBoilerplateApplicationDependency.cs   (AddWatherForecastApplicationModule)
services.AddScoped<IContractService, ContractService>();

// EDITAR  Adapters/Driving/Apis/.../ConsigBoilerplateApiDependency.cs      (AddWatherForecastApiModule)
services.AddScoped<IContractAppService, ContractAppService>();

// EDITAR  Adapters/Driven/.../ConsigBoilerplateDatabaseDependency.cs       (AddWatherForecastDatabaseModule)
services.AddBmgScopedRepository<IContractRepository, ContractRepository>();

// EDITAR  Adapters/Driven/.../UnitOfWork/v1/UnitOfWorkOracle.cs  (+ a interface Interfaces/v1/IUnitOfWorkOracle.cs)
public IContractRepository Contracts => _provider.GetRequiredService<IContractRepository>();

// EDITAR  Adapters/Driven/.../UnitOfWork/v1/UnitOfWorkOracleContext.cs     (scaffolding EF, [Obsolete])
public DbSet<Contract> Contracts { get; set; }
```

#### Etapa 8 — Testes + validação
- Criar `Tests/Core/Application/.../Services/v1/ContractServiceTest.cs` e `Tests/.../Api.Test/v1/ContractControllerTest.cs`.
- **Integração externa?** Siga **"Checklist de arquivos — nova integração"** (inclui criar o `.csproj`, `dotnet sln add` e referenciá-lo no `.Api`).
- Validar: `dotnet build` **e** `SWAGGER_GENERATION=true dotnet build`.

### Checklist de arquivos — nova entidade (`Contract`)

**CRIAR**: `Domain/Models/v1/ContractModel.cs` · `Domain/Services/v1/IContractService.cs` · `Application/Services/v1/ContractService.cs` · `Database/Repositories/Interfaces/v1/IContractRepository.cs` · `Database/Repositories/v1/ContractRepository.cs` · `Database/Entities/v1/Contract.cs` · `Api/Dtos/v1/Contract/ContractRequest.cs` + `ContractResponse.cs` · `Api/Validators/v1/Contract/ContractRequestValidator.cs` · `Api/AppServices/v1/Interfaces/IContractAppService.cs` + `Api/AppServices/v1/ContractAppService.cs` · `Api/Controllers/v1/ContractController.cs` · testes em `Tests/`.

**EDITAR** (passo mais esquecido — 7 arquivos):

| Arquivo | O que adicionar |
|---|---|
| `Application/ConsigBoilerplateApplicationDependency.cs` | `AddScoped<IContractService, ContractService>()` |
| `Api/ConsigBoilerplateApiDependency.cs` | `AddScoped<IContractAppService, ContractAppService>()` |
| `Database/ConsigBoilerplateDatabaseDependency.cs` | `AddBmgScopedRepository<IContractRepository, ContractRepository>()` |
| `Database/UnitOfWork/Interfaces/v1/IUnitOfWorkOracle.cs` | `IContractRepository Contracts { get; }` |
| `Database/UnitOfWork/v1/UnitOfWorkOracle.cs` | `public IContractRepository Contracts => _provider.GetRequiredService<IContractRepository>();` |
| `Database/UnitOfWork/v1/UnitOfWorkOracleContext.cs` | `public DbSet<Contract> Contracts { get; set; }` |
| `Application/Mappings/v1/ModelMappingProfile.cs` + `Api/Mappings/v1/DtoMappingProfile.cs` | `CreateMap` Model↔Entity e DTO↔Model |

> ✅ **Concluído quando** `dotnet build` compila **e** os 7 arquivos de EDIÇÃO contêm as novas linhas. ⚠️ Compilar **não** garante o registro: faltar um `AddScoped`/`AddBmgScopedRepository` compila e só falha em runtime (`Unable to resolve service...`). Confira manualmente cada `*Dependency.cs`.

---

## Testes unitários

**Stack**: xUnit. Os testes **espelham a árvore de produção** dentro de `Tests/` (cada projeto tem seu `*.Test` correspondente e um `Usings.cs` com os `global using`).

| Projeto de produção | Projeto de teste |
|---|---|
| `Bmg.ConsigBoilerplate.Application` | `Tests/Core/Application/Bmg.ConsigBoilerplate.Application.Test` |
| `Bmg.ConsigBoilerplate.Api` | `Tests/Adapters/Driving/Apis/Bmg.ConsigBoilerplate.Api.Test` |
| `Bmg.ConsigBoilerplate.Metabusca` | `Tests/Adapters/Driven/.../Bmg.ConsigBoilerplate.Metabusca.Test` |
| `Bmg.ConsigBoilerplate.FaceTec` | `Tests/Adapters/Driven/.../Bmg.ConsigBoilerplate.FaceTec.Test` |

**Exemplo — teste de Domain Service** (`Application.Test/Services/v1/ConsigBoilerplateServiceTest.cs`):
```csharp
public class ConsigBoilerplateServiceTest
{
    private readonly Mock<IUnitOfWorkOracle> _uow = new();
    private readonly Mock<IFaceTecApiManager> _faceTec = new();

    private ConsigBoilerplateService MakeSut() => new(_uow.Object, _faceTec.Object);

    [Fact]
    public async Task GetWeatherAsync_deve_retornar_modelo_quando_existe()
    {
        // Arrange
        _uow.Setup(u => u.Weathers.SelectAsync(It.IsAny<CancellationToken>(), 1L))
            .ReturnsAsync(new Weather { Id = 1 });
        var sut = MakeSut();

        // Act
        var result = await sut.GetWeatherAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }
}
```

```bash
dotnet test                          # roda toda a suíte
dotnet test --collect:"XPlat Code Coverage"   # com cobertura (Coverlet)
```

---

## Governança de contratos (Sensedia Adaptive Governance)

Regras institucionais para o contrato OpenAPI (detalhe completo em `.github/copilot-instructions.md`):

- **Nome físico**: `sigla-nome-servico-api` em kebab-case (ex.: `wfcst-weather-forecast-api`). Consistente entre repositório, DNS (`*.cloudbmg.app.br`) e `basePath`.
- **Versionamento**: `info.version` só MAJOR; URI `/vN/recurso`. Breaking change → novo MAJOR.
- **URIs**: recursos de negócio no **plural**, **kebab-case**, sem verbos, máx. 3 níveis. Ex.: `/v1/relacionamento-clientes`.
- **Payloads**: propriedades em **camelCase**, datas ISO 8601 UTC, todo atributo com `type`/`description`/`example`.
- **Entidades (schemas)**: **PascalCase**; canônicas em `components/schemas`; ENUMs em `lower-kebab-case`.
- **HTTP status**: `200` GET/PUT/PATCH com body, `201` POST, `206` paginação, `204` DELETE, `400` contrato quebrado, `401` sem auth, `403` sem permissão, `404` recurso inexistente, `422` regra de negócio, `500` erro interno, `504` timeout gateway. **`400 ≠ 422`**. Coleção vazia → `200`, nunca `404`.
- **Paginação**: `_offset`/`_limit` em query, resposta `206` com `items` + metadados (nunca array na raiz). Ordenação `_asc`/`_desc`.
- **Headers**: kebab-case com iniciais maiúsculas; `x-bmg-id` (correlation id) injetado pelo `Bmg.Api.Client`.
- **Idempotência**: `GET/PUT/DELETE` idempotentes; `POST`/`PATCH` não idempotentes → `Idempotency-Key` quando necessário.

---

## Segurança, Observabilidade e Workers

**Segurança (`Bmg.Auth`)**: `UseBmgAuth()` obrigatório fora do DEV; autenticação via Entra ID. Proteja endpoints sensíveis com `[Authorize(Roles = "rle-...")]`. Segredos só via `Bmg.Parameter.Manager` (CNFG) — `appsettings` apenas DEV.

**Observabilidade (`Bmg.Logging.Internal`)**: `AddBmgLoggingInternal()`/`UseBmgLoggingInternal()` para logs JSON; `[BmgDynatraceTrace]` obrigatório em métodos públicos de Service e AppService; correlation id `x-bmg-id` propagado; `/healthz` exposto pelo `AddBmgApiProjectDependencies` (probe EKS).

**Workers / Background services**: `BmgScheduleBackgroundService` (jobs agendados) e `BmgBackgroundService` (consumidores contínuos, ex.: Kafka). Health checks embutidos; sinalize `WorkerStateService.Unhealthy()` quando degradado.

---

## Boas práticas

### ✅ Fazer
- Manter o `Domain` puro (sem EF, sem Dapper, sem referência a `Application`/`Database`/`Api`).
- Fazer mapeamento DTO ↔ Model **somente** no `AppService`; regra de negócio **somente** no `Application/Services`.
- Herdar das classes base: `BmgServiceBase`, `BmgAppServiceBase<I>`, `BmgControllerBase<I>`, `GenericRepository<,>`.
- Versionar tudo em `v{n}/`; criar `v2/` apenas em breaking change (sem alterar o contrato existente).
- Guardar todo módulo de infra com `if (!isSwaggerMode)` no `Program.cs`.
- Usar `Notifier.NotifyAsync` para violação de negócio (→ 422); `TransactionScope` para múltiplas escritas.
- Acessar dados com Dapper via `GenericRepository` + `SqlBuilder` parametrizado.
- Aplicar `[BmgDynatraceTrace]` em Services e AppServices; usar `Bmg.Parameter.Manager` para segredos.
- Espelhar a estrutura em `Tests/` e validar `SWAGGER_GENERATION=true dotnet build` antes do PR.

### ❌ Não fazer
- Usar `MediatR` (ADR-003) ou `ErrorOr` (ADR-004) — proibidos.
- Colocar lógica de negócio no Controller ou no AppService.
- Acessar `Database`/`Integrations` diretamente da `Api`, ou infra direta no `Domain`.
- Lançar `Exception` para validação de negócio (use `Notifier` → 422).
- Duplicar `*Model` (Domain) e `*Entity` (Database) com as mesmas propriedades só para mapear (duplicidade Sonar) — ver regra anti-duplicidade abaixo.
- Configurar geração de Swagger localmente no `.csproj` (`GenerateOpenApiFiles`, etc.) — vem da `Bmg.Project.Utils`.
- Acoplar o Swagger ao `Domain` (ex.: `IncludeXmlComments(...Domain.xml)`).
- Deixar segredos em `appsettings` fora do DEV.

> **Regra anti-duplicidade (Sonar)**: quando `WeatherModel` e a entity `Weather` têm as mesmas propriedades, use o **Domain Model diretamente como `TEntity`** do repositório (`GenericRepository<DatabaseConnection, WeatherModel>`) e `[Column("...")]` para divergência de coluna — evita o `CreateMap<WeatherModel, WeatherEntity>()` que origina a violação. Separe a Entity apenas quando o Model tem comportamento, ou a tabela tem campos de infra (audit/softdelete) sem equivalente no Model.

---

## Convenções de nomenclatura

| Tipo | Convenção | Exemplo |
|---|---|---|
| Port de entrada (Domain) | `I{Nome}Service` em `Domain/Services/v{n}/` | `IConsigBoilerplateService` |
| Implementação (Application) | `{Nome}Service` em `Application/Services/v{n}/` | `ConsigBoilerplateService` |
| Interface do AppService | `I{Nome}AppService` em `Api/AppServices/v{n}/Interfaces/` | `IConsigBoilerplateAppService` |
| AppService | `{Nome}AppService` em `Api/AppServices/v{n}/` | `ConsigBoilerplateAppService` |
| Controller | `{Nome}Controller` em `Api/Controllers/v{n}/` | `ConsigBoilerplateController` |
| Port de saída (repo) | `I{Entidade}Repository` em `Domain/Adapters/` ou `Database/Repositories/Interfaces/v{n}/` | `IWeatherRepository` |
| Repositório | `{Entidade}Repository` em `Database/Repositories/v{n}/` | `WeatherRepository` |
| Domain Model | `{Nome}Model` (record) em `Domain/Models/v{n}/` | `WeatherModel` |
| Entity (banco) | `{Entidade}` em `Database/Entities/v{n}/` | `Weather` |
| DTO de entrada/saída | `{Nome}Request` / `{Nome}Response` em `Api/Dtos/v{n}/{Nome}/` | `WeatherRequest` |
| Validator | `{Nome}RequestValidator` em `Api/Validators/v{n}/{Nome}/` | `WeatherRequestValidator` |
| Integração externa | `{Sistema}ApiManager` (+ `I...`) | `MetabuscaApiManager` |
| Módulo de DI | `{Contexto}{Camada}Dependency` / `Add{Contexto}{Camada}Module()` | `ConsigBoilerplateApiDependency` |
| Teste | `{Nome}Test` espelhando a árvore em `Tests/` | `ConsigBoilerplateServiceTest` |
| Versionamento | subpasta `v{n}/` em toda camada | `v1/`, `v2/` |

---

## Comandos disponíveis

```bash
dotnet restore                                    # restore de pacotes (Nexus interno)
dotnet build                                      # build padrão
dotnet run --project Adapters/Driving/Apis/Bmg.ConsigBoilerplate.Api/Bmg.ConsigBoilerplate.Api.csproj
dotnet test                                       # testes unitários
dotnet test --collect:"XPlat Code Coverage"       # testes com cobertura
dotnet tool restore                               # ferramentas (necessário p/ gerar swagger)
SWAGGER_GENERATION=true dotnet build              # gera swagger-specs/swagger-v*.json
```

---

## Checklist pré-PR

- [ ] Port de entrada em `Domain/Services/v{n}/I{Nome}Service.cs` (herda `IBmgServiceBase`)
- [ ] Implementação em `Application/Services/v{n}/{Nome}Service.cs` (herda `BmgServiceBase`, `[BmgDynatraceTrace]`)
- [ ] `I{Nome}AppService` + `{Nome}AppService` em `Api/AppServices/v{n}/` (herda `BmgAppServiceBase<I>`)
- [ ] Controller em `Api/Controllers/v{n}/` (herda `BmgControllerBase<I>`)
- [ ] DTOs em `Api/Dtos/v{n}/{Nome}/` e Validators em `Api/Validators/v{n}/{Nome}/`
- [ ] AutoMapper: `ModelMappingProfile` (Model↔Entity) e `DtoMappingProfile` (DTO↔Model)
- [ ] Repositório em `Database/Repositories/v{n}/` (herda `GenericRepository`) + registro no `UnitOfWork`
- [ ] Módulos de infra guardados por `if (!isSwaggerMode)` no `Program.cs`
- [ ] `Bmg.Parameter.Manager` para HML/PROD; `appsettings` apenas DEV
- [ ] `UseBmgAuth()` ativo fora do DEV; endpoints sensíveis com `[Authorize(Roles = "...")]`
- [ ] Teste unitário espelhando a estrutura em `Tests/`
- [ ] `SWAGGER_GENERATION=true dotnet build` gera `swagger-specs/` sem erro

---

## Troubleshooting

**`SWAGGER_GENERATION=true dotnet build` quebra** → algum módulo de infra está fora do `if (!isSwaggerMode)`. Mova banco/Kafka/CNFG/auth/clients externos para dentro do guard.

**`swagger-specs/` não é gerado** → falta `dotnet tool restore`, ou há configuração local de geração no `.csproj` (`GenerateOpenApiFiles`, `Microsoft.Extensions.ApiDescription.Server`). A geração deve vir da `Bmg.Project.Utils` (`buildTransitive`).

**API retorna 401 em todo endpoint** → `UseBmgAuth()` ativo sem token válido. Em DEV, o bloco de auth fica fora do fluxo; em HML/PROD, envie o JWT corporativo.

**Parâmetro/segredo nulo em HML/PROD** → não registrado no CNFG via `Bmg.Parameter.Manager` (`AddBmgParametersSecrets/Application/Brokers`). `appsettings` não vale fora do DEV.

**Sonar acusa duplicidade de código** → `*Model` e `*Entity` idênticos com `CreateMap` entre eles. Aplique a regra anti-duplicidade (Model como `TEntity`).

**Validação retornando 400 quando deveria ser 422** → regra de negócio sendo validada no DTO/Validator. Validação de contrato → 400 (Validator); regra de negócio → `Notifier.NotifyAsync` no `Application` → 422.

**Rota não versionada / 404 inesperado** → faltou `v{n}/` na subpasta ou `[ApiVersion]`/`[Route("v{version:apiVersion}/...")]` no Controller.

**`AddConsigBoilerplate{Nome}Module()` não existe / build quebra ao registrar integração** → faltou o `<ProjectReference>` do projeto de integração no `Bmg.ConsigBoilerplate.Api.csproj`. Toda integração criada precisa ser referenciada pelo `.Api`.
