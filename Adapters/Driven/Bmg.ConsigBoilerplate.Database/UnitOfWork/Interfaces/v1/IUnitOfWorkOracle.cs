using Bmg.ConsigBoilerplate.Database.Repositories.Interfaces.v1;

namespace Bmg.ConsigBoilerplate.Database.UnitOfWork.Interfaces.v1
{
    public interface IUnitOfWorkOracle
    {
        /// <summary>
        /// TBL_WTH
        /// </summary>
        IWeatherRepository Weathers { get; }
    }
}
