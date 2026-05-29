using Bmg.ConsigBoilerplate.Database.Entities.v1;
using Bmg.ConsigBoilerplate.Database.UnitOfWork.Interfaces.v1;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics.CodeAnalysis;

namespace Bmg.ConsigBoilerplate.Database.UnitOfWork.v1
{
    [ExcludeFromCodeCoverage]
    [Obsolete("O uso de EF Core não está liberado no Banco Bmg, está classe é para que caso em uma futura liberação a transição seja menos impactante. Por hora este UnitOfWork não deve ser utilizado!")]
    public class UnitOfWorkOracleContext : DbContext, IUnitOfWorkOracleContext
    {
        public UnitOfWorkOracleContext(DbContextOptions<UnitOfWorkOracleContext> options) : base(options) { }

        // TODO: MAPEIE OS REPOSITÓRIOS DAS SUAS ENTIDADES COMO DBSET
        public DbSet<Weather> Weathers { get; set; }

        public DatabaseFacade GetDatabase()
        {
            return Database;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
