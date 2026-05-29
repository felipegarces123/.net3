using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.FaceTec.v1;
using Bmg.ConsigBoilerplate.FaceTec.v1;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Bmg.ConsigBoilerplate.FaceTec
{
    [ExcludeFromCodeCoverage]
    public static class ConsigBoilerplateFaceTecDependency
    {
        public static void AddConsigBoilerplateFaceTecModule(this IServiceCollection services)
        {
            services.AddScoped<IFaceTecApiManager, FaceTecApiManager>();
        }
    }
}
