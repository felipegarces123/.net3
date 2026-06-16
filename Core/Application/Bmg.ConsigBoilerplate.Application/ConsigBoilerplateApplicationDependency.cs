using Bmg.ConsigBoilerplate.Application.Services.v1;
using Bmg.ConsigBoilerplate.Domain.Services.v1;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Bmg.ConsigBoilerplate.Application
{
    [ExcludeFromCodeCoverage]
    public static class ConsigBoilerplateApplicationDependency
    {
        public static void AddConsigBoilerplateApplicationModule(this IServiceCollection services)
        {
            services.AddScoped<Domain.Services.v1.IConsigBoilerplateService, Services.v1.ConsigBoilerplateService>();
        }
    }
}
