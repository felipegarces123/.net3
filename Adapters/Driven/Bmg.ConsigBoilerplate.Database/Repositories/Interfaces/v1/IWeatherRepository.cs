using Bmg.Connection.Manager.Data;
using Bmg.ConsigBoilerplate.Database.Entities.v1;
using Bmg.ConsigBoilerplate.Domain;

namespace Bmg.ConsigBoilerplate.Database.Repositories.Interfaces.v1
{
    public interface IWeatherRepository : IGenericRepository<DatabaseConnection, Weather>
    {

    }
}
