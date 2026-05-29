---
mode: agent
description: Quality-Gate de Swagger — valida o swagger.json gerado pelo Bmg.Project.Utils na pipeline, reporta violações de maturidade Sensedia Adaptive Governance e sugere correções nas anotações C# do template API.
---

# Prompt — Swagger Quality Gate (Pipeline / Sensedia Adaptive Governance)

## Objetivo

Validar o `swagger.json` gerado automaticamente pelo `Bmg.Project.Utils` via `AddBmgApiProjectDependencies` na pipeline de CI/CD.

O resultado desta validação é enviado ao **Sensedia Adaptive Governance** como métrica de maturidade de API.  
Violações aqui impactam o score do serviço no portal de governança do banco.

**Contexto de execução:** stage `quality-gate` da pipeline, após `SWAGGER_GENERATION=true dotnet build`.

---

## Como usar

Cole o conteúdo do `swagger-specs/swagger.json` e solicite:  
*"Execute o quality-gate neste swagger"*

O Copilot irá reportar todas as violações com severidade, localização e a correção necessária no código C# do template.

---

## Regras de Validação

### 🔴 CRÍTICO — bloqueia aprovação no Adaptive Governance

#### C1 — Nome físico da API (info.title / servers)
- `info.title` deve refletir o nome lógico da Arquitetura Funcional (domínio de negócio, não sigla técnica)
- O basePath em `servers` deve seguir `/{sigla-nome-servico-api}/v{MAJOR}` em kebab-case
- **Correção**: ajustar `AddBmgApiProjectDependencies` — o nome físico é derivado do `appsettings.json` (`ServiceName`)

#### C2 — Versionamento lógico (`info.version`)
- Deve conter **apenas o MAJOR** (`"1"`, `"2"`) — não `"1.0.0"`, não `"v1"`
- **Correção**: revisar configuração do Swashbuckle em `AddBmgApiProjectDependencies`

#### C3 — URIs com verbos ou formato de mídia
- Nenhum path pode conter verbos (`/buscar`, `/criar`, `/deletar`, `/get`, `/post`)
- Nenhum path pode conter extensão de formato (`.json`, `.xml`)
- **Correção**: renomear a rota no `[HttpGet]` / `[Route]` do Controller

#### C4 — Status code 400 usado para erro de negócio
- Se um endpoint documenta apenas `400` para erros, sem `422`, é violação crítica
- `400` = contrato quebrado; `422` = regra de negócio violada
- **Correção**: adicionar `[ProducesResponseType(typeof(IEnumerable<BmgNotification>), StatusCodes.Status422UnprocessableEntity)]` no Controller

#### C5 — Ausência de `summary` ou `description` em operações
- Todo endpoint deve ter `summary` (uma linha) e `description` (comportamento detalhado)
- **Correção**: adicionar `/// <summary>` e `/// <remarks>` no método do Controller

#### C6 — Schemas sem `description` ou `example`
- Todo atributo de schema deve ter `description` e `example`
- **Correção**: adicionar `/// <summary>` nas propriedades dos DTOs

---

### 🟡 ALERTA — reduz score de maturidade

#### A1 — PathParams não estão em kebab-case
- `{idCliente}`, `{id_cliente}` → deve ser `{id-cliente}`
- **Correção**: ajustar o parâmetro na rota do Controller — `[HttpGet("{id-cliente}")]` com `[FromRoute(Name = "id-cliente")] Guid idCliente`

#### A2 — QueryParams não estão em kebab-case
- `?dataInicio=`, `?data_inicio=` → deve ser `?data-inicio=`
- **Correção**: `[FromQuery(Name = "data-inicio")] DateTime dataInicio`

#### A3 — Entidades (schemas) não estão em PascalCase
- `clienteResponse`, `contrato_request` → deve ser `ClienteResponse`, `ContratoRequest`
- **Correção**: renomear o DTO em C# (o nome da classe vira o nome do schema)

#### A4 — Propriedades JSON não estão em camelCase
- `data_nascimento`, `DataNascimento` sem `[JsonPropertyName]` → deve serializar como `dataNascimento`
- **Correção**: configurar `JsonSerializerOptions` com `CamelCase` (já configurado no `AddBmgApiProjectDependencies`) ou verificar se há `[JsonPropertyName]` incorreto

#### A5 — Datas sem formato ISO 8601 nos exemplos
- `example: "20/01/2025"`, `"2025/01/20"` → deve ser `"2025-01-20"` ou `"2025-01-20T15:30:00Z"`
- **Correção**: ajustar o `example` no XML doc do DTO

#### A6 — Ausência de `readOnly` em campos gerados pelo servidor
- `id`, `dataCriacao`, `dataAtualizacao` sem `readOnly: true`
- **Correção**: não incluir esses campos no request DTO; mantê-los apenas no response DTO

#### A7 — Resposta paginada retornando array na raiz
- Response body é `array` diretamente → deve ser objeto com `items`, `total`, `offset`, `limit`
- **Correção**: usar `PaginatedData<T>` do `arqc-project-utils` como tipo de retorno no AppService

#### A8 — Endpoint paginado sem status `206`
- GET com `_offset`/`_limit` retorna `200` em vez de `206`
- **Correção**: `[ProducesResponseType(typeof(PaginatedData<T>), StatusCodes.Status206PartialContent)]`

#### A9 — Ausência do header `x-bmg-id` documentado
- O correlation id `x-bmg-id` deve estar documentado como header opcional em todo endpoint
- **Correção**: adicionar parâmetro global via `SwaggerOperationFilter` no `AddBmgApiProjectDependencies`

#### A10 — ENUMs não estão em lower-kebab-case
- `ATIVO`, `Ativo`, `StatusAtivo` → deve ser `ativo`, `pendente`, `cancelado`
- **Correção**: usar `[JsonConverter(typeof(JsonStringEnumConverter))]` + naming convention lowercase nos valores do enum

---

### 🔵 INFORMATIVO — boa prática recomendada

#### I1 — `info.description` ausente ou muito curta (< 50 caracteres)
- Deve descrever o escopo funcional da API, alinhado à Arquitetura Funcional
- **Correção**: enriquecer a descrição no `AddBmgApiProjectDependencies` ou `appsettings.json`

#### I2 — Ausência de `required` nos schemas de request
- Campos obrigatórios do request devem estar declarados no array `required` do schema
- **Correção**: usar `[Required]` via FluentValidation ou `required` no DTO com `[JsonRequired]`

#### I3 — Ausência de `minLength`/`maxLength` em strings com restrição de tamanho
- Campos como CPF, CNPJ, CEP devem declarar limites explícitos
- **Correção**: usar `[MaxLength(11)]` no DTO — o Swashbuckle lê automaticamente

#### I4 — Operações sem tag agrupadora
- Tags organizam endpoints no portal do Sensedia
- **Correção**: o `BmgControllerBase` já injeta a tag pelo nome do Controller; verificar se está herdado corretamente

#### I5 — Ausência do campo `nullable: true` em campos opcionais do response
- Campos que podem retornar null devem declará-lo explicitamente
- **Correção**: `string? Campo { get; set; }` (nullable reference type) — o Swashbuckle converte para `nullable: true`

---

## Classificação Sensedia Adaptive Governance

O AG retorna um `scoreAverage` (0–100) com a seguinte classificação oficial:

| Score | Classificação | Cor | Impacto no processo |
|---|---|---|---|
| 95–100 | **Excellent** | 🟢 Verde `#107B3B` | ✅ **Selo Ouro de Arquitetura** — padrão de excelência |
| 80–94 | **Advanced** | 🔵 Azul `#1771C6` | ✅ **Selo Prata de Arquitetura** — aprovado com distinção |
| 30–79 | **Intermediate** | 🟡 Amarelo `#FFAD04` | ⚠️ **Warning** — deploy permitido com alertas visíveis no portal |
| 0–29 | **Basic** | 🔴 Vermelho `#D70026` | 🚫 **Bloqueio** — impede deploy / publicação no gateway Sensedia |

> **Regra de pipeline Bmg:**
> - `Basic` (< 30) → **bloqueia o merge/deploy**
> - `Intermediate` (30–79) → **warning** na pipeline, deploy permitido com registro de débito técnico
> - `Advanced` (80–94) → **selo prata**, publicado no portal de APIs do banco
> - `Excellent` (95–100) → **selo ouro**, destaque no catálogo corporativo

---

## Formato do Relatório

O AG retorna um JSON de resposta no seguinte formato real:

```json
{
  "scoreAverage": 68.5,
  "violations": [
    "Operation description \"\" is too short. Use more than 9 characters and clearly explain the operation.",
    "Parameter description \"\" is too short. Use more than 4 characters to explain its purpose.",
    "Schema BalanceErrorItem description is too short. Use more than 9 characters.",
    "Property code description \"\" is too short. Use at least 9 characters.",
    "Server URL basepath at position 0 is not compliant with the expected versioning format. Include an explicit version segment (e.g. /v1).",
    "Endpoint path \"/v1/saldos\" is not REST-compliant. Use patterns like /service or /service/{id}.",
    "API description is too short. Use more than 15 characters and describe the API objective, endpoints, and operations."
  ],
  "classification": {
    "name": "Intermediate",
    "color": "#FFAD04",
    "range": { "start": 30.0, "end": 79.0 }
  }
}
```

O Copilot deve interpretar este JSON e gerar o relatório estruturado abaixo:

```
═══════════════════════════════════════════════════════════════
  SWAGGER QUALITY GATE — {nome-do-servico-api} v{N}
  Arquivo: swagger/swagger.json
═══════════════════════════════════════════════════════════════

📊 SCORE: {scoreAverage}/100 — {classification.name}
  ├─ Excellent  (95–100) � Selo Ouro      → não atingido
  ├─ Advanced   (80–94)  🔵 Selo Prata     → não atingido
  ├─ Intermediate(30–79) 🟡 Warning        → ← ATUAL
  └─ Basic       (0–29)  � BLOQUEIO       → não atingido

RESULTADO DO PIPELINE: ⚠️ WARNING — deploy permitido com débito técnico

───────────────────────────────────────────────────────────────
VIOLAÇÕES IDENTIFICADAS PELO AG ({n} no total)
───────────────────────────────────────────────────────────────

Mapeamento de cada violação → causa no código C# + correção:

[V1] "Operation description "" is too short..."
  Endpoints afetados: GET /v1/email/envolvido/{uiEnvolvido}
  Causa: ausência de /// <remarks> no método do Controller
  Correção:
    /// <remarks>
    /// Retorna todos os e-mails vinculados ao envolvido informado.
    /// Utilize para consultar contatos antes de enviar notificações.
    /// </remarks>

[V2] "Server URL basepath at position 0 is not compliant..."
  Causa: servers[0].url não contém segmento de versão /vN
  Correção: verificar configuração do ServiceName/basePath em appsettings.json
  Esperado: https://sigla-nome-servico-api.cloudbmg.app.br/v1

[V3] "Property {nome} description "" is too short..."
  Causa: propriedade do DTO sem /// <summary> ou summary vazio
  Correção em cada DTO afetado:
    /// <summary>Identificador único do envolvido no formato UUID.</summary>
    public Guid UiEnvolvido { get; set; }

[V4] "Endpoint path "/v1/saldos" is not REST-compliant..."
  Causa: path não segue padrão /service ou /service/{id}
  Verificar: nome do recurso, hierarquia e presença de verbo na URI

[V5] "API description is too short..."
  Causa: info.description ausente ou < 15 caracteres
  Correção em appsettings.json ou configuração do Swashbuckle:
    "ApiDescription": "Gerencia e mantém os contatos de envolvidos do banco digital. ..."

───────────────────────────────────────────────────────────────
PRIORIDADE DE CORREÇÃO PARA AVANÇAR DE NÍVEL
───────────────────────────────────────────────────────────────

Para sair de Intermediate → Advanced (score ≥ 80):
  1. Adicionar description em todas as operações (maior impacto em volume)
  2. Adicionar description em todas as propriedades dos schemas
  3. Corrigir Server URL basepath para incluir /vN
  4. Corrigir endpoints não-REST-compliant

Para atingir Excellent (score ≥ 95):
  5. Adicionar description em todos os parâmetros
  6. Garantir description em todos os schemas (objetos)
  7. Revisar API description (info.description) com texto rico

═══════════════════════════════════════════════════════════════
  STATUS DO PIPELINE:
  Basic       → � BLOQUEIO de merge/deploy
  Intermediate→ ⚠️  WARNING com registro de débito técnico
  Advanced    → ✅  APROVADO com Selo Prata
  Excellent   → ✅  APROVADO com Selo Ouro
═══════════════════════════════════════════════════════════════
```

---

## Mapeamento AG violation → correção C# (referência rápida)

| Mensagem do AG | Causa no código | Correção |
|---|---|---|
| `Operation description "" is too short` | Método do Controller sem `/// <remarks>` | Adicionar `/// <remarks>Descrição detalhada...</remarks>` |
| `Parameter description "" is too short` | Parâmetro sem `/// <param name="x">` | Adicionar XML doc no parâmetro do método |
| `Property {x} description "" is too short` | Propriedade do DTO sem `/// <summary>` | Adicionar `/// <summary>Descrição...</summary>` na propriedade |
| `Schema {X} description is too short` | Classe DTO sem `/// <summary>` | Adicionar `/// <summary>` na declaração da classe |
| `API description is too short` | `info.description` ausente/curta | Enriquecer descrição no `appsettings.json` ou configuração Swashbuckle |
| `Server URL basepath...not compliant` | URL sem `/vN` no path | Corrigir `ServiceName`/`basePath` na configuração do `AddBmgApiProjectDependencies` |
| `Endpoint path "..." is not REST-compliant` | Verbo na URI ou path não segue padrão REST | Renomear rota no `[Route]` / `[HttpGet]` do Controller |

---

## Integração com pipeline

```yaml
# azure-pipelines.yml — stage quality-gate
- stage: QualityGate
  jobs:
  - job: SwaggerValidation
    steps:
    - script: dotnet tool restore
    - script: |
        SWAGGER_GENERATION=true dotnet build --no-restore -q
      displayName: 'Gerar swagger.json'
    - script: |
        # O swagger.json em swagger/swagger.json
        # é publicado como artefato e enviado ao Sensedia Adaptive Governance
      displayName: 'Publicar contrato OpenAPI'
```

---

## Referência cruzada

- Para criar um contrato antes do desenvolvimento → use `api-design-first.prompt.md`
- Para documentar Controllers/DTOs existentes → use `document-swagger.prompt.md`
- Referência completa de governança → `api-contract-governance.prompt.md`
