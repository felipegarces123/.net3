using Microsoft.AspNetCore.Builder;
using InMemory = Microsoft.EntityFrameworkCore.InMemoryDatabaseFacadeExtensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bmg.ConsigBoilerplate.Database.UnitOfWork.Interfaces.v1;

namespace Bmg.ConsigBoilerplate.Database
{
    public static class ConsigBoilerplateMemoryDatabase
    {
        public static void AddInMemoryDatabase(IServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetService<IUnitOfWorkOracleContext>();

            if (context != null && InMemory.IsInMemory(context.GetDatabase()))
            {
                context.Weathers.Add(new Entities.v1.Weather
                {
                    Id = 1,
                    Date = DateTime.Now,
                    Summary = "Teste",
                    TemperatureC = 27
                });

                context.SaveChanges();
            }
        }
    }
}
