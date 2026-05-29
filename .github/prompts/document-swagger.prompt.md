# Prompt — Documentar endpoint para geração do Swagger

## Contexto

Neste projeto o Swagger é gerado automaticamente pelo `arqc-project-utils` via `AddBmgApiProjectDependencies`.  
O XML de documentação já está habilitado no `.csproj` (`GenerateDocumentationFile=true`).  
A qualidade do Swagger depende **exclusivamente das anotações no código** — Controller, DTOs e AppService interface.

---

## Instrução ao Copilot

Dado o **Controller** e os **DTOs** abaixo, aplique todas as anotações necessárias para gerar um Swagger completo e correto seguindo os padrões Bmg.

Regras obrigatórias:

### 1. Controller — XML summary em todo endpoint

```csharp
/// <summary>
/// Descrição clara do que o endpoint faz (uma linha).
/// </summary>
/// <param name="id">Identificador único do recurso.</param>
/// <param name="cancellationToken">Token de cancelamento da requisição.</param>
/// <returns>Recurso encontrado ou 204 se não existir.</returns>
/// <response code="200">Recurso retornado com sucesso.</response>
/// <response code="204">Recurso não encontrado.</response>
/// <response code="422">Erros de negócio — lista de <see cref="BmgNotification"/>.</response>
/// <response code="500">Erro interno do servidor.</response>
```

### 2. Controller — ProducesResponseType com tipo de retorno explícito

```csharp
// ✅ CORRETO — tipo explícito em cada response
[HttpGet("{id}", Name = nameof(GetByIdAsync))]
[ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public async Task<ActionResult<WeatherResponse>> GetByIdAsync(...)

// ❌ ERRADO — sem tipo no ProducesResponseType
[ProducesResponseType(StatusCodes.Status200OK)]
```

### 3. Controller — atributos de classe obrigatórios

```csharp
/// <summary>
/// Gerencia os recursos de [NomeDoRecurso].
/// </summary>
/// <response code="400">Erros de validação de campos.</response>
/// <response code="422">Erros de regras de negócio.</response>
/// <response code="500">Erros internos do servidor.</response>
[ApiController]
[ApiVersion("1")]
[Route("v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(IEnumerable<BmgNotification>), StatusCodes.Status422UnprocessableEntity)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class [Nome]Controller : BmgControllerBase<I[Nome]AppService>
```

### 4. DTOs de Request — validações com DataAnnotations ou FluentValidation

```csharp
/// <summary>Requisição para criação de ConsigBoilerplate.</summary>
public record WeatherRequest
{
    /// <summary>Temperatura em graus Celsius. Mínimo: -100. Máximo: 100.</summary>
    /// <example>25</example>
    [Required]
    [Range(-100, 100)]
    public int TemperatureC { get; init; }

    /// <summary>Descrição resumida das condições climáticas.</summary>
    /// <example>Ensolarado</example>
    [Required]
    [MaxLength(200)]
    public string Summary { get; init; } = string.Empty;
}
```

### 5. DTOs de Response — `<example>` em toda propriedade

```csharp
/// <summary>Dados de retorno de ConsigBoilerplate.</summary>
public record WeatherResponse
{
    /// <summary>Identificador único do registro.</summary>
    /// <example>42</example>
    public long Id { get; init; }

    /// <summary>Temperatura em graus Celsius.</summary>
    /// <example>25</example>
    public int TemperatureC { get; init; }

    /// <summary>Temperatura em Fahrenheit (calculada).</summary>
    /// <example>77</example>
    public int TemperatureF { get; init; }

    /// <summary>Descrição resumida das condições climáticas.</summary>
    /// <example>Ensolarado</example>
    public string Summary { get; init; } = string.Empty;
}
```

### 6. Endpoint de paginação — parâmetros documentados

```csharp
/// <summary>
/// Retorna lista paginada de ConsigBoilerplate.
/// </summary>
/// <param name="pageSize">Quantidade de itens por página. Máximo definido em configuração.</param>
/// <param name="pageNumber">Número da página atual. Inicia em 1.</param>
/// <param name="cancellationToken">Token de cancelamento.</param>
/// <response code="200">Página com itens e metadados de paginação.</response>
/// <response code="204">Nenhum item encontrado para os parâmetros informados.</response>
[HttpGet("paginated", Name = nameof(GetPaginatedAsync))]
[ProducesResponseType(typeof(PaginatedData<WeatherResponse>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public async Task<ActionResult<PaginatedData<WeatherResponse>>> GetPaginatedAsync(
    [FromQuery][Range(1, int.MaxValue)] int pageSize,
    [FromQuery][Range(1, int.MaxValue)] int pageNumber,
    CancellationToken cancellationToken)
```

### 7. Endpoint PATCH — JsonPatchDocument documentado

```csharp
/// <summary>
/// Atualiza parcialmente um ConsigBoilerplate pelo id.
/// </summary>
/// <remarks>
/// Utiliza o padrão JSON Patch (RFC 6902).
/// Exemplo de body:
/// <code>
/// [
///   { "op": "replace", "path": "/summary", "value": "Nublado" }
/// ]
/// </code>
/// </remarks>
/// <response code="200">Atualização aplicada com sucesso.</response>
/// <response code="422">Erros de negócio.</response>
[HttpPatch("{id}", Name = nameof(PatchAsync))]
[ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
[Consumes("application/json-patch+json")]
public async Task<ActionResult<bool>> PatchAsync(
    [FromRoute] long id,
    [FromBody] JsonPatchDocument<WeatherRequest> request,
    CancellationToken cancellationToken)
```

---

## Como usar este prompt

Cole o código do seu Controller + DTOs após a linha abaixo e peça ao Copilot para aplicar as regras acima:

```
Aplique todas as anotações de Swagger (XML summary, ProducesResponseType com tipos, exemplos nos DTOs) 
seguindo as regras do prompt document-swagger neste Controller e seus DTOs:

[COLE O CÓDIGO AQUI]
```
