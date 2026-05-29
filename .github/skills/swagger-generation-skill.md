# Skill Agent — Geração Estática de Swagger (API/BFF)

## Objetivo

Padronizar e automatizar a geração estática de OpenAPI/Swagger em projetos API/BFF, com foco em:

- gerar contratos no build (`dotnet build`) sem `dotnet run` manual;
- evitar dependências de infraestrutura no modo de geração;
- publicar artefatos prontos para pipeline;
- manter compatibilidade com versionamento de API.

## Contexto obrigatório

- Biblioteca base: `Bmg.Project.Utils` versão mínima 10.2.0.
- Modo de geração: variável `SWAGGER_GENERATION=true`.
- Saída padrão (ordem de prioridade):
  1. `$(BUILD_ARTIFACTSTAGINGDIRECTORY)/swagger-specs`
  2. `$(SolutionDir)/swagger-specs`
  3. `$(MSBuildProjectDirectory)/swagger-specs`
- Arquivos de saída:
  - `swagger-v*.json` (por versão)
  - `swagger.json` (alias da **maior versão**)

## Regras de execução do agente

1. **Nunca** depender de UI/endpoint Swagger exposto para gerar contrato.
2. Reaproveitar o pipeline de filtros e configurações de `AddSwaggerGen(...)`.
3. Em modo `SWAGGER_GENERATION=true`, reduzir acoplamento com infra externa.
4. Evitar mudanças grandes em APIs legadas; preferir ajuste mínimo e seguro.
5. Se projeto já herda `buildTransitive` da `Bmg.Project.Utils`, **não duplicar** target local de geração.

## Playbook de ação

### Etapa 1 — Diagnóstico

- Verificar se o projeto é `Microsoft.NET.Sdk.Web`.
- Verificar se usa `PackageReference` de `Bmg.Project.Utils`.
- Verificar se já existe target local `GenerateSwaggerOnBuild`.
- Verificar se `Program.cs` possui guarda **local** para `SWAGGER_GENERATION`, sem depender de parâmetro da Utils.
- Exemplo esperado:

  `var isSwaggerMode = string.Equals(Environment.GetEnvironmentVariable("SWAGGER_GENERATION"), "true", StringComparison.OrdinalIgnoreCase);`

### Etapa 2 — Ajustes mínimos

- Se faltar guarda no `Program.cs`, adicionar lógica para não registrar infra externa no modo swagger.
- Se existir duplicidade entre target local e transitive, remover duplicidade.
- Se projeto já usa `Bmg.Project.Utils` com `buildTransitive`, remover do `.csproj` dependências e alvos locais de geração (ex.: `Microsoft.Extensions.ApiDescription.Server`, `GenerateOpenApiFiles`, `OpenApiDocument`, targets de rename/cache).
- Garantir que `swagger.json` seja alias da maior versão (não fixo em v1).

### Ressalva de arquitetura (Hexagonal)

- **Não usar** `ConfigureSwaggerGen(...IncludeXmlComments(...Domain.xml))` no `Program.cs` para carregar comentários XML de projeto `Domain`.
- Esse acoplamento do adapter de entrada (`Driving/API`) com metadados de `Domain` é um **anti-pattern** para o template e viola separação de responsabilidades da arquitetura hexagonal.
- **Recomendação**: manter o contrato OpenAPI orientado ao boundary da API (controllers/DTOs do adapter de entrada).
- Se faltar documentação no contrato, priorizar mover/anotar os modelos expostos no boundary da API, em vez de referenciar XML de assemblies internos de domínio.

### Etapa 3 — Validação

Executar e validar:

1. Build normal.
2. Build com `SWAGGER_GENERATION=true`.
3. Conferir diretório de saída.
4. Conferir `swagger-v*.json` e `swagger.json`.

### Etapa 4 — Pipeline

- Garantir variável `SWAGGER_GENERATION=true` no build.
- Publicar `swagger-specs` como artifact.
- Falhar job se `swagger.json` não existir.

## Critérios de aceite

- Build em modo swagger sem erro.
- Sem necessidade de infra externa para geração.
- Contrato gerado por versão + alias estável.
- Pipeline publicando artifact corretamente.

## Respostas esperadas do agente

Ao finalizar uma tarefa, responder com:

1. Arquivos alterados;
2. O que foi consolidado;
3. Resultado de build/teste;
4. Caminho final do `swagger.json`;
5. Próximos passos (se houver).
