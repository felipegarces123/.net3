using Bmg.Api.Client.Extensions;
using Bmg.Auth.Extensions;
using Bmg.Cache.Manager.Extensions;
using Bmg.Logging.Internal.Extensions;
using Bmg.Parameter.Manager.Extensions;
using Bmg.Project.Utils;
using Bmg.Project.Utils.Extensions;
using Bmg.Project.Utils.Provider;
using Bmg.ConsigBoilerplate.Application;
using Bmg.ConsigBoilerplate.Database;
using Bmg.ConsigBoilerplate.FaceTec;
using Bmg.ConsigBoilerplate.Metabusca;
using Microsoft.OpenApi;
using System.Diagnostics.CodeAnalysis;

namespace Bmg.ConsigBoilerplate.Api
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        private const string ApplicationPrefix = "wfcst"; // TODO: Substitua pela sigla da aplicação
        private const string ApplicationName = "weather-forecast-api"; // TODO: Substitua pelo nome da aplicação sem a sigla no início

        public static async Task<int> Main(string[] args)
        {
            var app = default(WebApplication);

            try
            {
                BmgProjectUtils.SetProjectExecutionFolder();

                var builder = WebApplication.CreateBuilder(args);

                // ──────────────────────────────────────────────────────────────────
                // QUALITY GATEWAY - Ativa o modo de geração estática do Swagger para evitar acoplamento com infra externa durante o build.
                // ──────────────────────────────────────────────────────────────────                
                var isSwaggerMode = string.Equals(Environment.GetEnvironmentVariable("SWAGGER_GENERATION"), "true", StringComparison.OrdinalIgnoreCase);

                builder.AddBmgLoggingInternal();

                // ──────────────────────────────────────────────────────────────────
                // MÓDULOS CORE — sempre registrados, inclusive na geração do Swagger
                // Registra Controllers, AppServices e interfaces — necessário para
                // o Swashbuckle CLI descobrir os endpoints e gerar o contrato.
                // ──────────────────────────────────────────────────────────────────
                builder.Services.AddAutoMapper(
                    typeof(Application.Mappings.v1.ModelMappingProfile),
                    typeof(Mappings.v1.DtoMappingProfile)
                );

                builder.Services.AddConsigBoilerplateApiModule();
                builder.Services.AddBmgMemoryCacheManager();

                // ──────────────────────────────────────────────────────────────────
                // MÓDULOS DE INFRAESTRUTURA — ignorados na geração do Swagger
                // Dependem de CNFG, banco, brokers ou APIs externas indisponíveis
                // no ambiente de build. Ver: variável local isSwaggerMode
                // ──────────────────────────────────────────────────────────────────
                if (!isSwaggerMode)
                {
                    builder.Configuration.AddBmgParameterManagerSetup(ApplicationPrefix, ApplicationName)
                        .AddBmgParametersSecrets()
                        .AddBmgParametersApplication()
                        .AddBmgParametersBrokers();

                    builder.Services.AddConsigBoilerplateDatabaseModule(builder.Configuration);
                    // TODO: Descomente e remova a linha acima caso utilize banco NoSql
                    //builder.Services.AddConsigBoilerplateNoSqlDatabaseModule(builder.Configuration);

                    builder.Services.AddConsigBoilerplateApplicationModule();

                    builder.Services.AddBmgAuth(ApplicationPrefix, ApplicationName, builder.Configuration);

                    builder.AddBmgApiClient(builder.Configuration.GetValue<int>("ConsigBoilerplate.Api:ApiConsumptionTimeoutMs"));

                    builder.Services.AddConsigBoilerplateMetabuscaModule();
                    builder.Services.AddConsigBoilerplateFaceTecModule();

                    // TODO: Remova o registro de dependência e ConsigBoilerplateKafkaDependency.cs se o serviço não utilizar Kafka
                    //builder.Services.AddConsigBoilerplateKafkaModule(builder.Configuration);
                }

                // TODO: Ajuste as descrições do Swagger antes do primeiro build / deploy, seguindo as recomendações de governança e boas práticas de contratos API: https://orangebox.cloudbmg.app.br/techdocs/default/component/cdev/processos/governanca-apis/contratos-api-bmg-gov/
                app = builder.AddBmgApiProjectDependencies
                (
                    ApplicationPrefix,
                    ApplicationName,
                    typeof(Program),
                    "Weather Forecast API",
                    "An ASP.NET Core Web API for managing Weather Forecast items",
                    new Uri("https://example.com/terms"),
                    new OpenApiContact
                    {
                        Name = "Example Contact",
                        Url = new Uri("https://example.com/contact")
                    },
                    new OpenApiLicense
                    {
                        Name = "Example License",
                        Url = new Uri("https://example.com/license")
                    },
                    "This API version has been deprecated. Please use one of the new APIs available from the explorer.",
                    builder.Configuration.GetValue<int>("ConsigBoilerplate.Api:RateLimit:MaxRequests"),
                    builder.Configuration.GetValue<TimeSpan>("ConsigBoilerplate.Api:RateLimit:MaxRequestsWindow"),
                    builder.Configuration.GetValue<int>("ConsigBoilerplate.Api:MaxPagination"),
                    true
                );

                ConsigBoilerplateMemoryDatabase.AddInMemoryDatabase(app.Services); // TODO: Remova após configurar o banco de dados real

                // ──────────────────────────────────────────────────────────────────
                // PIPELINE CORE — sempre ativo
                // ──────────────────────────────────────────────────────────────────
                app.UseBmgLoggingInternal(app.Configuration);

                if (app.Environment.IsDevelopment())
                    app.UseDeveloperExceptionPage();

                // ──────────────────────────────────────────────────────────────────
                // PIPELINE DE INFRAESTRUTURA — ignorado na geração do Swagger
                // ──────────────────────────────────────────────────────────────────
                if (!isSwaggerMode)
                {
                    app.UseBmgApiClient();
                    app.UseBmgAuth();
                }

                await app.RunAsync();
            }
            catch (Exception ex)
            {
                if (app == null)
                {
                    Console.WriteLine(ex);
                    throw;
                }

                try
                {
                    var logger = app.Services.GetRequiredService<ILogger<BmgProjectLog>>();
                    logger.LogError(ex, ex.Message);
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine(ex);
                }

                return -1;
            }

            return 0;
        }
    }
}