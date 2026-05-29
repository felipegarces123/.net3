---
mode: agent
description: Design-First — cria o contrato OpenAPI (YAML/JSON) de uma API antes de qualquer desenvolvimento. Faça perguntas objetivas sobre o domínio e gere o swagger seguindo os padrões Bmg/Sensedia.
---

# Prompt — API Design First (Contrato antes do Código)

## Objetivo

Criar o contrato OpenAPI completo de uma API **antes de qualquer desenvolvimento**.  
Nenhuma biblioteca, build ou código .NET é necessário — apenas conhecimento do domínio de negócio.

Este prompt representa uma **mudança de mindset**: o contrato é o produto, o código é a implementação.  
O swagger gerado aqui serve como guia de desenvolvimento para o time e como artefato de entrada no Sensedia Adaptive Governance.

---

## Como usar

Responda as perguntas abaixo (pode ser de forma incremental). O Copilot irá gerar o contrato OpenAPI ao final.

---

## Perguntas de Levantamento

> O Copilot deve fazer estas perguntas ao desenvolvedor antes de gerar o contrato.  
> Pode ser respondido de uma vez ou em iterações.

### Bloco 1 — Identidade da API
1. **Qual o domínio de negócio?** (ex: Crédito Consignado, Conta Digital, Clientes PJ)
2. **Qual a sigla do serviço?** (4 caracteres registrados na Arquitetura Funcional — ex: `ABCD`)
3. **Qual o nome do serviço em kebab-case?** (ex: `credito-consignado`, `conta-digital`)
4. **Qual a versão MAJOR atual?** (ex: `1`, `2`)
5. **Qual a descrição funcional da API?** (o que ela oferece, quem consome)

### Bloco 2 — Operações
6. **Liste os recursos principais** (ex: clientes, contratos, simulacoes)
7. **Para cada recurso, quais operações são necessárias?**
   - Listar (GET coleção com filtros/paginação?)
   - Buscar por ID (GET unitário?)
   - Criar (POST?)
   - Atualizar integral (PUT?) ou parcial (PATCH?)
   - Remover (DELETE?)
8. **Há sub-recursos?** (ex: `/clientes/{id-cliente}/contratos`)

### Bloco 3 — Payloads e Regras
9. **Quais campos compõem cada entidade?** (nome, tipo, obrigatório, exemplo)
10. **Há campos somente leitura** (gerados pelo servidor, como `id`, `dataCriacao`)?
11. **Há ENUMs?** (valores fixos de domínio — ex: status do contrato)
12. **Quais as regras de validação mais importantes?** (tamanho máximo, formato, obrigatoriedade condicional)

### Bloco 4 — Segurança e Headers
13. **A API requer autenticação?** (padrão: sim, JWT via `Bmg.Auth`)
14. **Há headers customizados além do `x-bmg-id`** (injetado automaticamente pelo `Bmg.Api.Client`)?

---

## Geração do Contrato

Com base nas respostas, gerar o arquivo OpenAPI 3.0 seguindo **obrigatoriamente** as regras abaixo:

### Estrutura base obrigatória

```yaml
openapi: 3.0.1
info:
  title: "{Nome Lógico da API}"          # ex: API de Crédito Consignado
  description: "{Descrição funcional completa}"
  version: "{MAJOR}"                      # apenas o número: 1, 2, 3
servers:
  - url: "/{sigla-nome-servico-api}/v{MAJOR}"
    description: "Base path padrão Bmg"
```

### Regras de nomenclatura aplicadas automaticamente

| Elemento | Regra | Exemplo |
|---|---|---|
| `info.title` | Nome lógico do domínio | `API de Crédito Consignado` |
| `info.version` | MAJOR apenas | `1` |
| Nome físico (servers.url) | `sigla-nome-servico-api` kebab-case | `/abcd-credito-consignado-api/v1` |
| URIs (paths) | recursos kebab-case, sem verbos | `/clientes`, `/contratos-ativos` |
| PathParams | kebab-case | `{id-cliente}`, `{numero-contrato}` |
| QueryParams | kebab-case | `?data-inicio=`, `?status-contrato=` |
| Entidades (schemas) | PascalCase | `Cliente`, `Contrato`, `SimulacaoRequest` |
| Atributos (propriedades) | camelCase | `dataNascimento`, `numeroDocumento` |
| ENUMs | lower-kebab-case | `pendente`, `aprovado`, `cancelado` |
| Datas | ISO 8601 UTC | `"2025-01-20T15:30:00Z"` |

### Padrão de schema para cada atributo

```yaml
nomeAtributo:
  type: string          # obrigatório
  description: "..."    # obrigatório — orientado ao negócio
  example: "..."        # obrigatório — valor representativo
  minLength: 1          # quando aplicável
  maxLength: 100        # quando aplicável
  nullable: true        # explícito quando anulável
  readOnly: true        # campos gerados pelo servidor (id, dataCriacao)
  writeOnly: true       # campos apenas no request (senha, token)
```

### Status codes por operação

| Operação | Sucesso | Criação | Sem conteúdo | Lista vazia | Erro contrato | Erro negócio | Não encontrado |
|---|---|---|---|---|---|---|---|
| GET unitário | `200` | — | — | — | `400` | `422` | `404` |
| GET coleção/filtro | `200` ou `206`* | — | — | `200` + `[]` | `400` | `422` | — |
| POST | `200` | `201` | — | — | `400` | `422` | — |
| PUT | `200` | — | — | — | `400` | `422` | `404` |
| PATCH | `200` | — | — | — | `400` | `422` | `404` |
| DELETE | — | — | `204` | — | `400` | `422` | `404` |

*`206` obrigatório quando há paginação com `_offset`/`_limit`

### Padrão de resposta de erro (BmgNotification)

```yaml
BmgNotification:
  type: object
  properties:
    code:
      type: string
      description: Código do erro de negócio.
      example: "CLIENTE_NAO_ENCONTRADO"
    message:
      type: string
      description: Descrição do erro para o consumidor.
      example: "Cliente não encontrado para o identificador informado."
    field:
      type: string
      nullable: true
      description: Campo que originou o erro, quando aplicável.
      example: "numeroDocumento"
```

### Padrão de resposta paginada

```yaml
# Response body para GET com paginação
{NomeRecurso}PagedResponse:
  type: object
  properties:
    items:
      type: array
      items:
        $ref: '#/components/schemas/{NomeRecurso}'
    total:
      type: integer
      description: Total de registros disponíveis.
      example: 150
    offset:
      type: integer
      description: Posição inicial da página retornada.
      example: 0
    limit:
      type: integer
      description: Quantidade máxima de registros por página.
      example: 25
```

### Headers obrigatórios no contrato

```yaml
# Incluir em todo endpoint que requer autenticação
parameters:
  - name: Authorization
    in: header
    required: true
    description: Token JWT corporativo Bmg. Formato Bearer {token}.
    schema:
      type: string
      example: "Bearer eyJhbGciOiJSUzI1NiJ9..."
  - name: x-bmg-id
    in: header
    required: false
    description: Correlation ID para rastreabilidade. Injetado automaticamente pelo Bmg.Api.Client.
    schema:
      type: string
      format: uuid
      example: "550e8400-e29b-41d4-a716-446655440000"
```

---

## Exemplo de saída esperada

```yaml
openapi: 3.0.1
info:
  title: API de Clientes PJ
  description: |
    Gerencia o ciclo de vida de clientes pessoa jurídica do banco digital.
    Oferece operações de cadastro, consulta, atualização e inativação de clientes PJ.
  version: "1"
servers:
  - url: /abcd-clientes-pj-api/v1
    description: Base path padrão Bmg

paths:
  /clientes:
    get:
      summary: Lista clientes PJ com filtros e paginação
      description: Retorna a lista paginada de clientes. Utilize _offset e _limit para controlar a paginação.
      parameters:
        - name: _offset
          in: query
          required: false
          schema:
            type: integer
            default: 0
            example: 0
        - name: _limit
          in: query
          required: false
          schema:
            type: integer
            default: 25
            example: 25
        - name: status
          in: query
          required: false
          description: Filtra clientes pelo status. Aceita múltiplos valores.
          schema:
            type: string
            enum: [ativo, inativo, pendente]
            example: "ativo"
      responses:
        "206":
          description: Lista paginada de clientes retornada com sucesso.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ClientePagedResponse'
        "400":
          description: Requisição inválida — parâmetros mal formatados.
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/BmgNotification'
    post:
      summary: Cadastra novo cliente PJ
      description: Cria um novo registro de cliente pessoa jurídica.
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CadastroClienteRequest'
      responses:
        "201":
          description: Cliente cadastrado com sucesso.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Cliente'
        "400":
          description: Contrato inválido — campos obrigatórios ausentes ou tipos incorretos.
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/BmgNotification'
        "422":
          description: Regra de negócio violada — ex. CNPJ já cadastrado, dados inconsistentes.
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/BmgNotification'

  /clientes/{id-cliente}:
    get:
      summary: Consulta cliente por identificador
      parameters:
        - name: id-cliente
          in: path
          required: true
          description: Identificador único do cliente.
          schema:
            type: string
            format: uuid
            example: "550e8400-e29b-41d4-a716-446655440000"
      responses:
        "200":
          description: Cliente encontrado.
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Cliente'
        "404":
          description: Cliente não encontrado para o identificador informado.

components:
  schemas:
    Cliente:
      type: object
      properties:
        id:
          type: string
          format: uuid
          readOnly: true
          description: Identificador único gerado pelo servidor.
          example: "550e8400-e29b-41d4-a716-446655440000"
        razaoSocial:
          type: string
          description: Razão social da empresa.
          maxLength: 200
          example: "Empresa Exemplo S.A."
        cnpj:
          type: string
          description: CNPJ da empresa sem formatação.
          minLength: 14
          maxLength: 14
          example: "12345678000190"
        status:
          type: string
          enum: [ativo, inativo, pendente]
          description: Status atual do cliente.
          example: "ativo"
        dataCadastro:
          type: string
          format: date-time
          readOnly: true
          description: Data e hora do cadastro em UTC.
          example: "2025-01-20T15:30:00Z"

    CadastroClienteRequest:
      type: object
      required:
        - razaoSocial
        - cnpj
      properties:
        razaoSocial:
          type: string
          description: Razão social da empresa.
          maxLength: 200
          example: "Empresa Exemplo S.A."
        cnpj:
          type: string
          description: CNPJ da empresa sem formatação.
          minLength: 14
          maxLength: 14
          example: "12345678000190"

    ClientePagedResponse:
      type: object
      properties:
        items:
          type: array
          items:
            $ref: '#/components/schemas/Cliente'
        total:
          type: integer
          description: Total de clientes disponíveis.
          example: 150
        offset:
          type: integer
          example: 0
        limit:
          type: integer
          example: 25

    BmgNotification:
      type: object
      properties:
        code:
          type: string
          description: Código do erro de negócio.
          example: "CLIENTE_NAO_ENCONTRADO"
        message:
          type: string
          description: Descrição do erro para o consumidor.
          example: "Cliente não encontrado para o identificador informado."
        field:
          type: string
          nullable: true
          description: Campo que originou o erro, quando aplicável.
          example: "cnpj"
```

---

## Fluxo recomendado de uso

1. **Inicie** com: *"Quero criar o contrato da API de [domínio]"*
2. O Copilot fará as perguntas dos Blocos 1 a 4 (pode responder tudo de uma vez)
3. O Copilot gerará o YAML completo
4. **Itere**: *"Adicione o endpoint de atualização parcial"*, *"Adicione o campo telefone ao Cliente"*
5. **Salve** como `api-contract.yaml` ou `swagger.json` na raiz do projeto de documentação
6. Use o contrato como **guia de desenvolvimento** para implementar os Controllers e DTOs
