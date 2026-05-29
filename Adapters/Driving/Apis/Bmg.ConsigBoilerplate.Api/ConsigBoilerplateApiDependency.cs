using System.Diagnostics.CodeAnalysis;

namespace Bmg.ConsigBoilerplate.Api
{
    [ExcludeFromCodeCoverage]
    public static class ConsigBoilerplateApiDependency
    {
        public static void AddWatherForecastApiModule(this IServiceCollection services)
        {
            services.AddScoped<AppServices.v1.Interfaces.IConsigBoilerplateAppService, AppServices.v1.ConsigBoilerplateAppService>();
        }
    }
}
