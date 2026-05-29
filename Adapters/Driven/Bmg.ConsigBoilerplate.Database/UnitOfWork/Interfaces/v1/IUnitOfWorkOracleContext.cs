using Bmg.ConsigBoilerplate.Database.Entities.v1;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Bmg.ConsigBoilerplate.Database.UnitOfWork.Interfaces.v1
{
    [Obsolete("O uso de EF Core não está liberado no Banco Bmg, está classe é para que caso em uma futura liberação a transição seja menos impactante. Por hora este UnitOfWork não deve ser utilizado!")]
    public interface IUnitOfWorkOracleContext
    {
        DatabaseFacade GetDatabase();

        int SaveChanges();

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// TBL_WTH
        /// </summary>
        DbSet<Weather> Weathers { get; }
    }
}
