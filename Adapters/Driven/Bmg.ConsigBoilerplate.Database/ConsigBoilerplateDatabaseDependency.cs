using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Bmg.Connection.Manager.Extensions;
using Bmg.ConsigBoilerplate.Domain;
using System.Diagnostics.CodeAnalysis;

namespace Bmg.ConsigBoilerplate.Database
{
    [ExcludeFromCodeCoverage]
    public static class ConsigBoilerplateDatabaseDependency
    {
        public static void AddConsigBoilerplateDatabaseModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<UnitOfWork.Interfaces.v1.IUnitOfWorkOracle, UnitOfWork.v1.UnitOfWorkOracle>();

            // TODO: INSIRA SUAS CONEXÕES COM O BANCO DE DADOS
            services.AddBmgConnectionManager<DatabaseConnection>()
                .AddConnection<UnitOfWork.Interfaces.v1.IUnitOfWorkOracleContext, UnitOfWork.v1.UnitOfWorkOracleContext>
                (
                    DatabaseConnection.Oracle, // TODO: DEVE SER ÚNICO
                    (provider, options) =>
                    {
                        options.UseInMemoryDatabase("teste_api");
                    }
                )
                //.AddConnection<UnitOfWork.Interfaces.v1.IUnitOfWorkOracleContext, UnitOfWork.v1.UnitOfWorkOracleContext>
                //(
                //    DatabaseConnection.Oracle,
                //    (provider, options) =>
                //    {
                //        options.UseOracle(configuration.GetConnectionString("ExadataConnection"));
                //    }
                //)
                ;

            // TODO: INJETE SEUS REPOSITÓRIOS
            services.AddBmgScopedRepository<Repositories.Interfaces.v1.IWeatherRepository, Repositories.v1.WeatherRepository>();
        }
    }
}
