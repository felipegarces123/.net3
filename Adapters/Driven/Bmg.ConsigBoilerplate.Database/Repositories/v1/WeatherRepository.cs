using Bmg.Connection.Manager.Data;
using Bmg.ConsigBoilerplate.Database.Entities.v1;
using Bmg.ConsigBoilerplate.Database.Repositories.Interfaces.v1;
using Bmg.ConsigBoilerplate.Domain;
using Dapper;

namespace Bmg.ConsigBoilerplate.Database.Repositories.v1
{
    public class WeatherRepository : GenericRepository<DatabaseConnection, Weather>, IWeatherRepository
    {
        public WeatherRepository(IServiceProvider provider) : base(DatabaseConnection.Oracle, provider) { }

        public override async Task<Weather> SelectAsync(CancellationToken cancellationToken, params object[] ids)
        {
            var builder = new SqlBuilder();

            var template = builder.AddTemplate(@"
                                                 SELECT 
                                                 * 
                                                 FROM 
                                                 teste.tbl_wth
                                                 /**where**/
                                              ");

            builder.Where("wth_id = @Id");

            var result = await Connection.QueryFirstOrDefaultAsync<Weather>(template.RawSql,
                new
                {
                    Id = ids[0]
                });

            return result;
        }
    }
}
