using Bmg.Connection.Manager.Data;
using Bmg.ConsigBoilerplate.Domain.Models.v1;
using Bmg.Project.Utils.Interfaces;
using Microsoft.AspNetCore.JsonPatch;
using Bmg.Project.Utils.Data;

namespace Bmg.ConsigBoilerplate.Domain.Services.v1
{
    public interface IConsigBoilerplateService : IBmgServiceBase
    {

        Task<IEnumerable<WeatherModel>> GetWeathersAsync(CancellationToken cancellationToken);

        Task<PaginatedData<WeatherModel>> GetWeathersPaginatedAsync(int pageSize, int pageNumber, CancellationToken cancellationToken);

        Task<WeatherModel> GetWeatherAsync(long id, CancellationToken cancellationToken);        

        Task<WeatherModel> CreateWeatherAsync(WeatherModel weather, CancellationToken cancellationToken);

        Task<bool> PatchWeatherAsync(long id, JsonPatchDocument<WeatherModel> weatherPatch, CancellationToken cancellationToken);

        Task<bool> UpdateWeatherAsync(WeatherModel weather, CancellationToken cancellationToken);

        Task<bool> DeleteWeatherAsync(long id, CancellationToken cancellationToken);
    }
}
