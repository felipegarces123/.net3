Sumário dos tópicos
- Visão geral
- Princípios e padrões arquiteturais
- Estrutura de pastas e finalidade técnica de cada camada (com exemplos)
- Versionamento e OpenAPI (Swagger)
- Exemplo prático de Program.cs (com guarda isSwaggerMode)
- Injeção de dependências e módulos (Template BMG) — exemplo
- Gerenciamento de parâmetros e segredos (Parameter Manager) — exemplo
- Persistência e repositórios (Dapper / GenericRepository) — exemplo
- AppServices, Domain Services e Controllers — exemplo
- Integrações externas e Bmg.Api.Client — exemplo de uso
- Logging, tracing e observabilidade (Dynatrace)
- Segurança robusta (Bmg.Auth, Entra ID, roles)
- Workers e background services
- CI/CD, geração de artefatos e governança de contrato
- CRUD, endpoints e convenções de rota
- Checklist pré-PR e governança operacional

1. Visão geral
- Arquitetura: Hexagonal (Ports & Adapters). Separação clara: Api (Driving) → Application → Domain ← Driven (Database/Integrations).
- Prioridades: contratos versionados, segurança, observabilidade e gestão centralizada de parâmetros.

2. Princípios e padrões arquiteturais
- Domain puro: sem dependência de infra.
- Application implementa regras e orquestração; não acessa infraestrutura diretamente (usa ports/interfaces).
- AppService (Api) é único responsável por mapear DTO ↔ DomainModel e orquestrar chamadas ao Domain.
- Controller: apenas delega ao AppService e trata respostas HTTP.
- Evitar MediatR e ErrorOr; usar classes base corporativas (BmgServiceBase, BmgAppServiceBase, BmgControllerBase).
- Todos os contratos públicos versionados em subpastas v{n}.

3. Estrutura de pastas e finalidade técnica (detalhado)
- Adapters/Driving/Apis/Bmg.ConsigBoilerplate.Api/
  - Objetivo: Expor endpoints HTTP/REST, receber requisições e traduzir para DTOs. Contém Controllers, AppServices de orquestração de fluxo, DTOs/Validators e Program.cs.
  - Exemplo de organização:
	- Controllers/v1/
	- AppServices/v1/
	- Dtos/v1/
	- Validators/
	- Program.cs

- Core/Application/Bmg.ConsigBoilerplate.Application/
  - Objetivo: Implementar casos de uso e regras de orquestração que dependem do domínio. Contém implementações de Services, UnitOfWork e mappings.
  - Exemplo de organização:
	- Services/v1/
	- Mappings/

- Core/Domain/Bmg.ConsigBoilerplate.Domain/
  - Objetivo: Definir o modelo de negócio puro e contratos (interfaces). Contém Models, interfaces de serviços (ports de entrada) e interfaces de adaptadores (ports de saída).
  - Exemplo de organização:
	- Models/
	- Services/v1/
	- Adapters/

- Adapters/Driven/Bmg.ConsigBoilerplate.Database/
  - Objetivo: Implementar os ports de saída definidos no Domain para persistência (repositories). Contém repositórios que usam Dapper/GenericRepository.
  - Exemplo de organização:
	- Repositories/v1/

- Integrations/
  - Objetivo: Implementar clients para sistemas externos (APIs, mensageria).

- Mappings/
  - Objetivo: AutoMapper profiles. AppService é o ponto de conversão DTO ↔ Domain.

- Tests/
  - Objetivo: testes unitários e de integração seguindo a estrutura acima.

4. Versionamento e OpenAPI
- Gerar OpenAPI na build com SWAGGER_GENERATION=true; produzir swagger-specs/swagger-v{n}.json e swagger-specs/swagger.json.
- info.version no OpenAPI: apenas MAJOR (1,2,...).
- Program.cs deve proteger inicialização de infra real com isSwaggerMode.

5. Exemplo prático de Program.cs (diretriz e exemplo)
```csharp
var builder = WebApplication.CreateBuilder(args);

// Evita inicializar infra durante geração de OpenAPI
var isSwaggerMode = string.Equals(
	Environment.GetEnvironmentVariable("SWAGGER_GENERATION"),
	"true",
	StringComparison.OrdinalIgnoreCase);

// Registrar dependências que NÃO dependem de infra
builder.Services.AddBmgApiProjectDependencies(builder.Configuration);
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddBmgLoggingInternal();

// Registrar módulos de domínio e app (sempre)
builder.Services.AddConsigBoilerplateApplicationModule();
builder.Services.AddConsigBoilerplateApiModule();

if (!isSwaggerMode)
{
	// Registrar infra apenas quando NÃO estamos gerando o OpenAPI
	builder.Services.AddConsigBoilerplateDatabase(builder.Configuration);
	builder.Services.AddBmgKafka(builder.Configuration);
	builder.Services.AddBmgApiClient<IExternalService, ExternalServiceClient>();
	builder.Services.AddParameterManager(builder.Configuration);
}

var app = builder.Build();

if (!isSwaggerMode)
{
	app.UseMiddleware<SomeInfraMiddleware>();
}

app.MapControllers();
app.Run();
```

6. Injeção de dependências e módulos (Template BMG) — exemplo
```csharp
public static class ConsigBoilerplateApiModule
{
	public static IServiceCollection AddConsigBoilerplateApiModule(this IServiceCollection services)
	{
		// Registrar AppServices, Controllers DI e mapeamentos específicos da API
		services.AddScoped<IConsigBoilerplateAppService, ConsigBoilerplateAppService>();
		services.AddAutoMapper(typeof(ConsigMappingProfile));
		return services;
	}
}

public static class ConsigBoilerplateApplicationModule
{
	public static IServiceCollection AddConsigBoilerplateApplicationModule(this IServiceCollection services)
	{
		services.AddScoped<IConsigBoilerplateService, ConsigBoilerplateService>();
		services.AddScoped<IUnitOfWork, ConsigUnitOfWork>();
		return services;
	}
}
```

7. Gerenciamento de parâmetros e segredos — exemplo de uso
```csharp
// Em Program.cs (registrar)
if (!isSwaggerMode)
{
	builder.Services.AddParameterManager(builder.Configuration);
}

// Em código de inicialização
var parameters = app.Services.GetRequiredService<IParameterManager>();
var mySecret = parameters.Get("/svc/consig/connection-string");
```

8. Persistência e repositórios — exemplo
```csharp
public interface IContractRepository : IGenericRepository<DatabaseConnection, ContractModel> { }

public class ContractRepository : GenericRepository<DatabaseConnection, ContractModel>, IContractRepository
{
	public ContractRepository(IServiceProvider provider) : base(DatabaseConnection.Oracle, provider) { }

	public override async Task<ContractModel> SelectAsync(CancellationToken ct, params object[] ids)
	{
		var builder = new SqlBuilder();
		var template = builder.AddTemplate("SELECT * FROM schema.tbl_contract /**where**/");
		builder.Where("contract_id = @Id", new { Id = ids[0] });
		return await Connection.QueryFirstOrDefaultAsync<ContractModel>(template.RawSql, new { Id = ids[0] });
	}
}
```

9. AppServices, Domain Service e Controller — exemplo
```csharp
[BmgDynatraceTrace]
public class ConsigBoilerplateAppService : BmgAppServiceBase<IConsigBoilerplateService>, IConsigBoilerplateAppService
{
	public ConsigBoilerplateAppService(IConsigBoilerplateService service, IMapper mapper) : base(service, mapper) { }

	public async Task<ConsigResponse> PostAsync(ConsigRequest request, CancellationToken ct)
	{
		var model = Mapper.Map<ConsigModel>(request);
		var result = await Service.CreateConsigAsync(model, ct);
		return Mapper.Map<ConsigResponse>(result);
	}
}
```

Exemplo Controller:
```csharp
[ApiController]
[Route("v1/[controller]")]
public class ConsigBoilerplateController : BmgControllerBase<IConsigBoilerplateAppService>
{
	public ConsigBoilerplateController(IConsigBoilerplateAppService appService) : base(appService) { }

	[HttpPost]
	public async Task<ActionResult<ConsigResponse>> Post(ConsigRequest request, CancellationToken ct)
	{
		var result = await AppService.PostAsync(request, ct);
		return Ok(result);
	}
}
```

10. Integrações externas e Bmg.Api.Client — exemplo de chamada segura
```csharp
await apiClient
	.Url("https://api-destino/rota-sensivel")
	.WithBmgSecuredData()
	.WithOAuthBearerToken(token.AccessToken)
	.PostJsonAsync(payload);
```

11. Logging, tracing e observabilidade
- Usar AddBmgLoggingInternal (IBmgLogging) para logs em JSON e integração com Dynatrace.
- Propagar correlation id (x-bmg-id) e aplicar [BmgDynatraceTrace] em métodos públicos.
- Expor /healthz e registrar health checks para conexões e Workers.

12. Segurança robusta (Bmg.Auth / Entra ID)
- UseBmgAuth() obrigatório fora do DEV; autenticação centralizada via Entra ID/Azure AD.
- Proteja endpoints sensíveis com [Authorize(Roles = "rle-...")].

13. Workers e background services
- Usar BmgScheduleBackgroundService para jobs agendados e BmgBackgroundService para consumidores contínuos.
- Health checks embutidos; sinalizar unhealthy via WorkerStateService.Unhealthy().

14. CI/CD e geração de artefatos
- Pipeline padrão: restore → build (SWAGGER_GENERATION=true em job específico) → test → publish artifacts (bin + swagger-specs).
- Publicar swagger-specs como artefato.

15. CRUD, endpoints e convenções
- Rotas padronizadas pelo AddBmgApiProjectDependencies (kebab-case, controller name mapping).
- Suportar GET, POST, PUT, PATCH, DELETE. PATCH deve aceitar operação e campo a atualizar.
- Paginação: _offset e _limit, limite máximo (ex.: 100). Resposta paginada usando PaginatedData<T> e status 206.

16. Checklist pré-PR
- [ ] Domain Service interface em Domain/Services/v1/
- [ ] Implementação em Application/Services/v1/ (BmgServiceBase)
- [ ] I{Nome}AppService e {Nome}AppService em Api/AppServices/v1/
- [ ] Controller em Api/Controllers/v1/ (BmgControllerBase)
- [ ] DTOs e Validators em Api/Dtos/v1/ e Api/Validators/
- [ ] AutoMapper profiles configurados e AppService executando o mapping
- [ ] AddParameterManager para HML/PROD; appsettings apenas DEV
- [ ] AddBmgLoggingInternal e [BmgDynatraceTrace] aplicados
- [ ] UseBmgAuth() ativo fora do DEV e endpoints sensíveis com [Authorize(Roles = "...")]
- [ ] Geração de swagger-specs validada

Notas finais
- Consulte o Nexus (NuGet interno) para versões compatíveis de Bmg.* para .NET 10.
- Mantenha a separação de responsabilidades e versionamento estrito: criar v2 apenas quando houver breaking change.