# [Sigla] — [Nome do Serviço] API

> **[Descrição de uma linha: o que o serviço faz e para quem]**

| Campo              | Valor                                             |
|--------------------|---------------------------------------------------|
| **Status**         | Em Andamento / Em Produção / Depreciado            |
| **Squad**          | [Nome do time]                                    |
| **Pipeline**       | [URL do pipeline]                                 |
| **Dashboard**      | [URL do dashboard de monitoramento]               |

---

## 📑 Índice

- [Sobre o Serviço](#-sobre-o-serviço)
- [Arquitetura](#-arquitetura)
- [Como executar](#-como-executar)
- [Geração de Swagger (API First)](#-geração-de-swagger-api-first)
- [Configurações obrigatórias no Program.cs (API/BFF)](#️-configurações-obrigatórias-no-programcs-apibff)
- [Testes](#-testes)
- [Documentação Referência](#-documentação-referência)
- [Como contribuir](#-como-contribuir)

---

## 📖 Sobre o Serviço

[Descrição detalhada em 2–4 linhas: problema que resolve, domínio de negócio, quem consome.]

### Funcionalidades

- [x] [Recurso principal]
- [x] [Recurso secundário]
- [ ] [Recurso planejado]

---

## 🏗️ Arquitetura

Hexagonal (Ports & Adapters) — 4 camadas:

```
Core/
  Domain/         → Entidades, interfaces de serviço, regras de negócio
  Application/    → Implementação dos serviços de domínio
Adapters/
  Driving/        → API (Controllers, AppServices, DTOs, Validators)
  Driven/         → Database, Integrações externas, Kafka, etc.
```

[Link para diagrama de arquitetura]

---

## ▶️ Como executar

```bash
# Restore de pacotes
dotnet restore

# Executar em modo desenvolvimento
dotnet run --project Adapters/Driving/Apis/Bmg.[Contexto].Api/Bmg.[Contexto].Api.csproj
```

---

## 📄 Geração de Swagger sem dependência de Regra de Negócio e registros externos

O contrato OpenAPI é gerado **automaticamente no build** quando `SWAGGER_GENERATION=true`.  

> Pré-requisito: utilizar `Bmg.Project.Utils` (mínimo `10.2.0`) com suporte `buildTransitive`.
>
> No template, a geração deve vir da Utils (sem configuração local de geração no `.csproj`, como `Microsoft.Extensions.ApiDescription.Server`, `GenerateOpenApiFiles` ou targets de rename/cache).

Saída padrão (ordem de precedência):

1. `$(BUILD_ARTIFACTSTAGINGDIRECTORY)/swagger-specs` (pipeline)
2. `$(SolutionDir)/swagger-specs` (build via `.sln`)
3. `$(MSBuildProjectDirectory)/swagger-specs` (build via `.csproj`)

Arquivos esperados:

- `swagger-v*.json` (por versão)
- `swagger.json` (alias da maior versão)

### Gerar localmente (antes do PR)

```bash
dotnet tool restore
SWAGGER_GENERATION=true dotnet build
# → swagger-specs/swagger-v*.json e swagger-specs/swagger.json gerados automaticamente
```

### Regra obrigatória no `Program.cs`

Todo módulo de infraestrutura (banco, Kafka, CNFG, APIs externas) **deve ser guardado**:

```csharp
// ✅ Correto
var isSwaggerMode = string.Equals(
  Environment.GetEnvironmentVariable("SWAGGER_GENERATION"),
  "true",
  StringComparison.OrdinalIgnoreCase);

if (!isSwaggerMode)
{
    builder.Services.AddMeuServicoDatabase(builder.Configuration);
    builder.Services.AddBmgKafka(...);
}
```

### Ressalva de arquitetura (Hexagonal)

- Não acoplar a API ao `Domain` para enriquecer Swagger (ex.: `ConfigureSwaggerGen(...IncludeXmlComments(...Domain.xml))` no `Program.cs`).
- O contrato OpenAPI deve refletir o boundary da API (`Driving`), com documentação em controllers/DTOs expostos.

---

## ⚙️ Configurações obrigatórias no Program.cs (API/BFF)

Checklist mínimo para manter geração de contrato estável e aderência arquitetural:

1. **Guarda local de modo Swagger**

```csharp
var isSwaggerMode = string.Equals(
   Environment.GetEnvironmentVariable("SWAGGER_GENERATION"),
   "true",
   StringComparison.OrdinalIgnoreCase);
```

2. **Infra externa protegida por guarda** (`if (!isSwaggerMode)`)
  - Banco de dados
  - Kafka / mensageria
  - Auth externa
  - API clients externos

3. **Não executar middleware de infra no modo Swagger**
  - Ex.: `UseBmgApiClient`, `UseBmgAuth` dentro de `if (!isSwaggerMode)`.

4. **Configurar corretamente a `ApiProjectDependencies` para a API em execução**
  - Garantir as chaves de configuração:
    - `ConsigBoilerplate.Api:RateLimit:MaxRequests`
    - `ConsigBoilerplate.Api:RateLimit:MaxRequestsWindow`
    - `ConsigBoilerplate.Api:MaxPagination`
  - Exemplo no `Program.cs`:

```csharp
builder.Configuration.GetValue<int>("ConsigBoilerplate.Api:RateLimit:MaxRequests"),
builder.Configuration.GetValue<TimeSpan>("ConsigBoilerplate.Api:RateLimit:MaxRequestsWindow"),
builder.Configuration.GetValue<int>("ConsigBoilerplate.Api:MaxPagination"),
```

5. **Não acoplar Swagger ao Domain**
  - Evitar `IncludeXmlComments(...Domain.xml)` no `Program.cs`.
  - Documentar no boundary de entrada (`Driving`) para preservar a arquitetura hexagonal.

> Essas regras valem para **API e BFF**.

---

## 🧪 Testes

```bash
dotnet test
```

---

## 📚 Documentação Referência

- Desenvolvimento Backend  
  https://orangebox.cloudbmg.app.br/techdocs/default/component/cdev/padroes/desenvolvimento/backend/

- Governança API  
  https://orangebox.cloudbmg.app.br/techdocs/default/component/cdev/processos/governanca-apis/contratos-api-bmg-gov/

- Tutorial .NET Bmg  
  https://orangebox.cloudbmg.app.br/techdocs/default/component/cdev/tutoriais/dotnet/dotnet-index/

---

## 🤝 Como contribuir

1. Verifique as regras em `.github/copilot-instructions.md`
2. Crie uma branch a partir de `main`
3. Implemente seguindo a arquitetura hexagonal
4. Certifique-se que `SWAGGER_GENERATION=true dotnet build` não quebra
5. Abra o PR com descrição clara das mudanças
