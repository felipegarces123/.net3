# Archetype 3 — Arquitetura Ultra-Enterprise (Guia Completo)
Versão avançada do archetype para o projeto Bmg.ConsigBoilerplate. Conteúdo em português, orientado a equipes corporativas que trabalham com .NET 10, Hexagonal (Ports & Adapters) e com as bibliotecas corporativas Bmg (arqc-*).
Observações do workspace
- Projeto: Bmg.ConsigBoilerplate
- Linguagem: C# (.NET 10)
- Local do Program.cs: Adapters/Driving/Apis/Bmg.ConsigBoilerplate.Api/Program.cs
- Convenções corporativas: Bmg.Project.Utils, Bmg.Api.Client, Bmg.Auth, Bmg.Connection.Manager, Bmg.Logging.Internal
Sumário
- Visão geral
- Padrões arquiteturais e responsabilidades
- Estrutura de pastas recomendada
- Convenções de versionamento e OpenAPI
- Exemplo de Program.cs (guardas de infra e geração de Swagger)
- Boas práticas de persistência (Dapper / GenericRepository)
- AppServices, Domain Services e Controllers — contratos e responsabilidades
- Integrações externas e clients (AddBmgApiClient)
- Observabilidade, tracing e logs (Dynatrace)
- Segurança, secrets e CNFG
- Testes, CI/CD e geração de artefatos
- Checklist de entrega/PR
1. Visão geral
Arquitetura Hexagonal estrita para garantir desacoplamento entre negócio e infra. A API deve ser definida API-First (OpenAPI) e manter contratos versionados por MAJOR (v1, v2 ...).
2. Padrões arquiteturais e responsabilidades
- Domain: puro, sem dependência de infra. Contém Models, Interfaces (Ports de entrada), Adapters (Ports de saída - interfaces).
- Application: implementa regras de negócio, depende de Domain, usa Ports de saída.
- Adapters/Driving (Api): Controllers, AppServices, DTOs, Validators.
- Adapters/Driven: Database (repositories), Integrations (clients externos).
Regras obrigatórias:
- Não usar MediatR ou ErrorOr.
- Services e AppServices públicos devem ter [BmgDynatraceTrace].
- Todas as classes base: BmgServiceBase, BmgAppServiceBase, BmgControllerBase.
- Não injetar infra diretamente em Domain.
3. Estrutura de pastas (exemplo)
- Adapters/Driving/Apis/Bmg.ConsigBoilerplate.Api/ 		# Expor endpoints HTTP/REST, receber requisições e traduzir para DTOs. Contém: Controllers, AppServices de orquestração de fluxo e DTOs/Validators. Nunca contém regras de negócio, delega ao Application via AppService.
  - Controllers/v1/
  - AppServices/v1/
  - Dtos/v1/
  - Validators/
  - Program.cs											# Registrar dependências, configurar middleware, autenticação, logging, OpenAPI e inicializar módulos de infra. Deve conter guardas para geração de Swagger (isSwaggerMode) e evitar inicializar infra real durante a geração de contrato.
- Core/Domain/Bmg.ConsigBoilerplate.Domain/ 			# Definir o modelo de negócio puro e contratos (interfaces). Contém Models, interfaces de serviços (ports de entrada) e interfaces de adaptadores (ports de saída) como repositórios e clientes externos. Não depende de nenhuma implementação de infraestrutura.
  - Models/
  - Services/v1/
  - Adapters/
- Core/Application/Bmg.ConsigBoilerplate.Application/	# Implementar casos de uso e regras de orquestração que dependem do domínio. Contém implementações de Services que usam Ports (interfaces) do Domain para acessar infra. É responsável por transações, coordenação entre repositórios e aplicação de políticas de negócio mais altas.
  - Services/v1/
  - Mappings/											# Mapear DTOs ↔ Domain Models e Domain Models ↔ Entities (quando justificável). Localizado em Application e Api quando aplicável; o AppService é o único ponto que transforma DTOs em Models e vice-versa.
- Adapters/Driven/Bmg.ConsigBoilerplate.Database/		# Implementar os ports de saída definidos no Domain para persistência (repositories). Contém repositórios que usam Dapper/GenericRepository e manipulam queries, mapeamentos e detalhes de conexão. Fornece dados para Application via interfaces.
  - Repositories/v1/
- Integrations/											# Implementar clients para sistemas externos (APIs, mensageria). Encapsula comunicação externa, tratamentos de retry/timeout e mapeamento para os ports do Domain.
- Tests/												# Agrupar testes unitários e de integração espelhando a estrutura da aplicação (Domain tests, Application tests, AppService/Controller tests). Isolar dependências externas usando mocks ou testcontainers.)
4. Versionamento e OpenAPI
- Cada contrato público (DTO, Controller, AppService, Service, Repository) deve existir em uma subpasta v{n}.
- Gerar OpenAPI via pipeline: SWAGGER_GENERATION=true dotnet build
- Swagger outputs: swagger-specs/swagger-v{n}.json e swagger-specs/swagger.json
- info.version = MAJOR somente (1,2,...)
- URIs: kebab-case, recursos nominais, max 3 níveis
- Paginação: _offset e _limit; retornar PaginatedData<T> com status 206
5. Exemplo de Program.cs (boas práticas — guardas para geração de Swagger)
```csharp
var builder = WebApplication.CreateBuilder(args);
// Variável local para evitar inicialização de infra durante geração de OpenAPI
var isSwaggerMode = string.Equals(
	Environment.GetEnvironmentVariable("SWAGGER_GENERATION"),
	"true",
	StringComparison.OrdinalIgnoreCase);
// Registrar dependências que NÃO dependem de infra (sempre)
builder.Services.AddBmgApiProjectDependencies(builder.Configuration);
builder.Services.AddAutoMapper(typeof(Program));
// Registrar módulos de domínio e app (sempre)
builder.Services.AddConsigBoilerplateApplicationModule();
builder.Services.AddConsigBoilerplateApiModule();
if (!isSwaggerMode)
{
	// Registrar infra apenas quando NÃO estamos gerando o OpenAPI
	builder.Services.AddConsigBoilerplateDatabase(builder.Configuration);
	builder.Services.AddBmgKafka(builder.Configuration);
	builder.Services.AddBmgApiClient<IExternalService, ExternalServiceClient>();
}
var app = builder.Build();
if (!isSwaggerMode)
{
	app.UseMiddleware<SomeInfraMiddleware>();
}
app.MapControllers();
app.Run();
```
6. Persistência — GenericRepository e Domain Model como TEntity
- Usar GenericRepository<TConnection, TEntity> do Bmg.Connection.Manager (Dapper + SqlBuilder).
- Preferir o Domain Model como TEntity para evitar duplicidade.
- Quando necessário, use atributos [Column("db_column")] no Domain Model.
- Exemplos de repositório:
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
7. AppService, Domain Service e Controller — responsabilidades
- AppService (Api): mapear DTO -> DomainModel, chamar I{Nome}Service, mapear DomainModel -> DTO (usar AutoMapper quando apropriado).
- Domain Service (Application): contém regras de negócio, usa IUnitOfWork/IRepository via Ports de saída.
- Controller: finos, apenas delegam ao AppService e retornam ActionResult apropriados.
Exemplo AppService:
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
8. Integrações externas
- Registrar via AddBmgApiClient<TInterface, TImplementation>() para garantir propagação de x-bmg-id e logging consistente.
- Configurar políticas de retry e rate-limit centralizadas.
9. Observabilidade e tracing
- Usar Bmg.Logging.Internal para logs estruturados.
- Marcar métodos públicos com [BmgDynatraceTrace].
- Propagar x-bmg-id em toda chamada externa (BmgTraceLogHandler).
- Expor /healthz para readiness/readiness probes.
- Instrumentar métricas e traces via OpenTelemetry quando aplicável.
10. Segurança e gestão de segredos
- Autenticação JWT via Bmg.Auth; controllers devem herdar BmgControllerBase e ter [Authorize] atuando por default.
- Configurações sensíveis via Bmg.Parameters.Manager (CNFG).
- HTTPS obrigatório; HSTS e headers de segurança em produção.
11. Testes e qualidade
- Testes unitários por camada (Domain, Application, Api/AppService).
- Testes de integração isolando infra com testcontainers ou mocks de GenericRepository.
- Integrar análise estática (SonarQube) no pipeline e evitar duplicidade Model <-> Entity.
12. CI/CD e geração de artefatos
- Pipeline:
  1. dotnet restore
  2. dotnet build (com SWAGGER_GENERATION=true em job específico para gerar swagger-specs)
  3. dotnet test
  4. publish artifacts (binários + swagger-specs)
- Versionamento de imagem alinhado ao MAJOR do contrato quando breaking change.
Comando local para gerar OpenAPI:
```powershell
$env:SWAGGER_GENERATION = 'true'
dotnet build
# artefatos em ./swagger-specs
```
13. Checklist pré-PR
- [ ] Interface do Domain Service em Domain/Services/v1/
- [ ] Implementação do Application Service em Application/Services/v1/ (herda BmgServiceBase)
- [ ] I{Nome}AppService e {Nome}AppService em Api/AppServices/v1/
- [ ] Controller em Api/Controllers/v1/ (herda BmgControllerBase)
- [ ] DTOs e Validators em Api/Dtos/v1/ e Api/Validators/
- [ ] Mapping configurado (AutoMapper) e evitar mapeamento Domain <-> Entity desnecessário
- [ ] [BmgDynatraceTrace] nos métodos públicos de serviço e appservice
- [ ] Não carregar infra em Program.cs quando SWAGGER_GENERATION=true
- [ ] Testes unitários e integração adicionados
14. Observações finais específicas ao workspace
- Verifique Program.cs em Adapters/Driving/Apis/Bmg.ConsigBoilerplate.Api: garanta o uso de isSwaggerMode e que AddBmgApiProjectDependencies é chamado antes de adicionar infra bloqueada.
- Confirmar compatibilidade dos pacotes Bmg.* com .NET 10.
Fim do archetype3 — arquivo gerado a partir das convenções do projeto e análise do workspace.