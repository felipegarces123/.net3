using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Bmg.ConsigBoilerplate.Domain;
using System.Diagnostics.CodeAnalysis;
using Bmg.NoSqlConnection.Manager.Extensions;
using MongoDB.Driver;

namespace Bmg.ConsigBoilerplate.Database
{
    [ExcludeFromCodeCoverage]
    public static class ConsigBoilerplateNoSqlDatabaseDependency
    {
        public static void AddWatherForecastNoSqlDatabaseModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<UnitOfWork.Interfaces.v1.IUnitOfWorkOracle, UnitOfWork.v1.UnitOfWorkOracle>();

            // TODO: INSIRA SUAS CONEXÕES COM O BANCO DE DADOS
            services.AddBmgNoSqlConnectionManager<DatabaseNoSqlConnection>()
                .AddConnection
                (
                    DatabaseNoSqlConnection.MongoDB, // TODO: DEVE SER ÚNICO
                    MongoClientSettings.FromConnectionString(configuration.GetConnectionString("MongoConnection"))
                );

            // TODO: INJETE SEUS REPOSITÓRIOS
            services.AddScoped<Repositories.Interfaces.v1.NoSql.IUserRepository, Repositories.v1.NoSql.UserRepository>();
        }
    }
}
