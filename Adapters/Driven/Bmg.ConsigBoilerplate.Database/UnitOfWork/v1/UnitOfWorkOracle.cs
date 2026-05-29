using Bmg.ConsigBoilerplate.Database.Repositories.Interfaces.v1;
using Bmg.ConsigBoilerplate.Database.UnitOfWork.Interfaces.v1;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Bmg.ConsigBoilerplate.Database.UnitOfWork.v1
{
    [ExcludeFromCodeCoverage]
    public class UnitOfWorkOracle : IUnitOfWorkOracle
    {
        private readonly IServiceProvider _provider;

        public UnitOfWorkOracle(IServiceProvider provider) => _provider = provider;

        // TODO: MAPEIE SEUS REPOSITÓRIOS
        public IWeatherRepository Weathers => _provider.GetRequiredService<IWeatherRepository>();
    }
}
