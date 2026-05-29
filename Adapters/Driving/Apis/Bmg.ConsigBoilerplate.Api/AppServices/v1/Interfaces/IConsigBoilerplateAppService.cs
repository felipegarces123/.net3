using Bmg.Connection.Manager.Data;
using Bmg.Project.Utils.Data;
using Bmg.ConsigBoilerplate.Api.Dtos.v1.ConsigBoilerplate;
using Microsoft.AspNetCore.JsonPatch;

namespace Bmg.ConsigBoilerplate.Api.AppServices.v1.Interfaces
{
    public interface IConsigBoilerplateAppService
    {
        Task<IEnumerable<WeatherResponse>> GetAsync(CancellationToken cancellationToken);

        Task<PaginatedData<WeatherResponse>> GetPaginatedAsync(int pageSize, int currentPage, CancellationToken cancellationToken);

        Task<WeatherResponse> GetAsync(long id, CancellationToken cancellationToken);

        Task<WeatherResponse> PostAsync(WeatherRequest request, CancellationToken cancellationToken);

        Task<bool> PatchAsync(long id, JsonPatchDocument<WeatherRequest> request, CancellationToken cancellationToken);

        Task<bool> PutAsync(WeatherRequest request, CancellationToken cancellationToken);

        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken);
    }
}
