---
mode: agent
description: Base de conhecimento — padrões e convenções de Governança de APIs Bmg/Sensedia. Referência consultada pelos prompts api-design-first e api-swagger-quality-gate.
---

# Referência — Governança de Contratos de API (Bmg / Sensedia)

## Contexto

No Bmg, a API é um **produto de primeira classe**. O contrato OpenAPI (`swagger.json`) é o artefato central da estratégia de **API First** — ele é gerado automaticamente na pipeline, publicado no Sensedia Adaptive Governance e avaliado por métricas de maturidade antes de qualquer deploy.

Este prompt instrui o Copilot a atuar como um **revisor de contrato de API** com profundo conhecimento das convenções internas do Bmg e das melhores práticas REST/OpenAPI.

---

## Instrução ao Copilot

Analise o contrato OpenAPI (ou os atributos Swashbuckle no código) fornecido e aplique / valide **todas** as regras abaixo.

---

## 1. Identidade e Nome da API

### `info.title` — Nome Lógico
- Deve refletir o domínio de negócio registrado na Arquitetura Funcional
- Sem siglas técnicas, versões ou prefixos de tecnologia
- ✅ `API de Crédito Consignado` | ❌ `ms-credito-v1`, `JavaCustomerAPI`

### Nome Físico (gateway Sensedia)
- Padrão: `sigla-nome-servico-api` em kebab-case (4 chars de sigla)
- Gerado automaticamente pelo `Bmg.Project.Utils`
- ✅ `abcd-clientes-api` | ❌ `ms-api-clientes`, `pushNotificationAPI`

### `info.version` — Versionamento Lógico
- Apenas o número **MAJOR** (`1`, `2`, `3`) — sem `MAJOR.MINOR.PATCH`
- Alinhado ao versionamento físico na URI

### URI — Versionamento Físico
- Padrão: `/vN/nome-servico-em-kebab-case`
- Exemplos: `/v1/clientes`, `/v3/relacionamento-clientes`

---

## 2. URIs

| Regra | ✅ Correto | ❌ Incorreto |
|---|---|---|
| Representar recurso, não ação | `/clientes` | `/buscarClientes` |
| Plural para coleções | `/cartoes` | `/cartao` |
| Singular para recurso único | `/perfil` | `/perfis` |
| Letras minúsculas | `/relacionamento-clientes` | `/RelacionamentoClientes` |
| kebab-case composto | `/conta-digital` | `/contaDigital`, `/conta_digital` |
| Máximo 3 níveis | `/clientes/{id}/cartoes/{id}/faturas` | `/a/{id}/b/{id}/c/{id}/d` |
| PathParams: kebab-case | `/{id-cliente}` | `/{idCliente}`, `/{id_cliente}` |
| QueryParams: kebab-case | `?id-usuario=` | `?idUsuario=`, `?id_usuario=` |
| Sem verbos na URI | `/clientes` (POST) | `/criar-cliente` |
| Sem formato de mídia na URI | `/relatorios` | `/relatorios.json` |

---

## 3. Payloads — Request e Response

### Propriedades dos schemas
```yaml
# ✅ CORRETO
dataNascimento:
  type: string
  format: date
  description: Data de nascimento do cliente no formato ISO 8601.
  example: "1990-05-20"

# ❌ INCORRETO — sem description, sem example, snake_case, abreviado
dt_nasc:
  type: string
```

**Regras obrigatórias:**
- `type` em todo atributo
- `description` clara e orientada ao negócio
- `example` representativo em todo atributo
- `minLength` / `maxLength` quando aplicável
- **camelCase** para nomes de propriedades JSON
- ❌ Sem snake_case, sem abreviações (`dt_nasc`, `docNum`)

### Datas e horas
- Data: `type: string`, `format: date`, `example: "1990-05-20"` (ISO 8601 `YYYY-MM-DD`)
- Data-hora: `type: string`, `format: date-time`, `example: "2025-01-20T15:30:00Z"` (sempre UTC/Zulu)

### Controle de leitura/escrita
```yaml
id:
  type: string
  readOnly: true   # retornado apenas no response
senha:
  type: string
  writeOnly: true  # enviado apenas no request
dataCancelamento:
  type: string
  nullable: true   # explícito quando anulável
```

### Payload orientado ao recurso (não a comandos)
```json
// ✅ CORRETO
{ "nome": "João Silva", "email": "joao@email.com" }

// ❌ INCORRETO — payload com ação
{ "criarCliente": true, "acao": "CADASTRAR" }
```

---

## 4. Entidades (components/schemas)

- **PascalCase** para nome da entidade: `Cliente`, `ContaDigital` ✅ | `ClienteFinanceiroOperacional` ❌
- **Entidades canônicas** (reutilizáveis entre operações): definir em `components/schemas`
- **Entidades não canônicas**: nomear com sufixo de contexto — `AlteracaoCotacaoRequest`, `SimulacaoResponse`
- **ENUMs**: lower-kebab-case com termos de negócio claros
  ```yaml
  statusCotacao:
    type: string
    enum: [pendente, aprovada, recusada, cancelada]
  ```
- Entidades somente inline (não reutilizadas): definir diretamente no `requestBody` ou `response` (não em `components`)

---

## 5. HTTP Status Codes — Semântica obrigatória

| Método | Situação | Status |
|---|---|---|
| GET | Sucesso com recurso | `200` |
| GET | Coleção com filtros vazia | `200` + lista vazia (nunca `404`) |
| GET | Recurso por URI específica não encontrado | `404` |
| GET | Resposta paginada | `206` (sempre, inclusive última página) |
| POST | Recurso criado | `201` |
| POST | Processamento sem criação direta | `200` |
| PUT | Recurso substituído | `200` |
| PATCH | Recurso alterado | `200` |
| DELETE | Recurso removido | `204` |
| Qualquer | Request quebrado (contrato inválido) | `400` |
| Qualquer | Validação semântica / regra de negócio | `422` |
| Qualquer | Não autenticado | `401` |
| Qualquer | Sem permissão | `403` |
| Qualquer | Erro interno | `500` |
| Qualquer | Timeout de gateway | `504` |

> **Regra crítica 400 vs 422:**
> - `400` = o request está estruturalmente quebrado (campo obrigatório ausente, tipo de dado errado)
> - `422` = o request é válido no contrato, mas a regra de negócio rejeitou o conteúdo

---

## 6. Headers

### Padrão de nomenclatura
- Headers HTTP padrão: **kebab-case com iniciais maiúsculas** (RFC 4229)
  - ✅ `Content-Type`, `Authorization`, `Cache-Control`
  - ❌ `content-type`, `authorization`
- Headers custom: mesma convenção, sem prefixo `x-` obrigatório
  - ✅ `Sistema-Operacional`, `Id-Usuario`
  - ❌ `SistemaOperacional`, `id_rastreio`

### Header obrigatório Bmg
- **`x-bmg-id`**: correlation id corporativo — injetado automaticamente pelo `Bmg.Api.Client`
  - ✅ `x-bmg-id`
  - ❌ `Correlation-Id`, `x-correlation-id`

### Documentação de headers no contrato
```yaml
# ✅ CORRETO — com description, example e required
parameters:
  - name: client_id
    in: header
    required: true
    description: client_id Sensedia para consumo da API no gateway.
    schema:
      type: string
      example: "550e8400-e29b-41d4-a716-446655440000"
```

---

## 7. Paginação

### QueryParams padrão
- `_offset`: posição inicial (integer ≥ 0)
- `_limit`: quantidade máxima de registros

```
GET /v1/produtos?_offset=0&_limit=25
```

### Response paginado
```json
{
  "items": [...],
  "total": 150,
  "offset": 0,
  "limit": 25
}
```

- Status: **sempre `206 Partial Content`** em respostas paginadas
- ❌ Nunca retornar array na raiz de respostas paginadas

### Filtros e ordenação
```
GET /v1/produtos?status=ativo
GET /v1/produtos?status=ativo&status=pendente
GET /v1/produtos?nome=*Cereal*
GET /v1/produtos?preco=lt:100
GET /v1/produtos?_asc=dataCriacao
GET /v1/produtos?_desc=dataCriacao
GET /v1/produtos?_fields=id,nome,preco
```

---

## 8. Idempotência

| Método | Idempotente? | Ação necessária |
|---|---|---|
| GET | ✅ Sempre | — |
| PUT | ✅ Sempre | — |
| DELETE | ✅ Sempre | — |
| POST | ❌ Por padrão | Usar header `Idempotency-Key` quando necessário |
| PATCH | ❌ Por definição | Garantir na implementação ou usar `Idempotency-Key` |

---

## 9. Checklist de revisão de contrato

Ao revisar ou gerar um contrato OpenAPI, valide:

- [ ] `info.title` reflete o nome lógico da Arquitetura Funcional
- [ ] `info.version` contém apenas o MAJOR (`1`, `2`, `3`)
- [ ] `info.description` descreve o escopo funcional completo
- [ ] Todas as URIs em kebab-case, sem verbos, sem versão no path além do `/vN`
- [ ] Todos os schemas têm `type`, `description` e `example`
- [ ] Propriedades JSON em camelCase, sem abreviações
- [ ] Datas em formato ISO 8601, horários em UTC
- [ ] `readOnly`/`writeOnly`/`nullable` declarados explicitamente onde aplicável
- [ ] Status codes semanticamente corretos (400 vs 422 revisado)
- [ ] `206` em toda resposta paginada
- [ ] Headers documentados com `description`, `example` e `required`
- [ ] `x-bmg-id` presente como header de rastreabilidade
- [ ] ENUMs em lower-kebab-case com valores de negócio
- [ ] Entidades reutilizáveis em `components/schemas`
- [ ] Todos os endpoints documentados com `summary` e `description`
- [ ] Todos os status codes de erro documentados no contrato

---

## Como usar este prompt

1. Cole o contrato OpenAPI (YAML/JSON) ou os atributos Swashbuckle do Controller/DTOs
2. Peça: *"Revise este contrato aplicando as regras de Governança Bmg"*
3. O Copilot irá identificar violações e propor as correções necessárias

**Exemplos de uso:**
- *"Revise as URIs deste contrato seguindo as convenções Bmg"*
- *"Corrija os status codes deste swagger — 422 vs 400"*
- *"Documente os headers obrigatórios neste contrato"*
- *"Revise os nomes das propriedades deste schema para camelCase"*
- *"Adicione paginação padrão Bmg neste endpoint de listagem"*
