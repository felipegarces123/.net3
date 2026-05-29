using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.Bmg.Metabusca.v1;
using Bmg.ConsigBoilerplate.Metabusca.v1;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Bmg.ConsigBoilerplate.Metabusca
{
    [ExcludeFromCodeCoverage]
    public static class ConsigBoilerplateMetabuscaDependency
    {
        public static void AddConsigBoilerplateMetabuscaModule(this IServiceCollection services)
        {
            services.AddScoped<IMetabuscaApiManager, MetabuscaApiManager>();
        }
    }
}
