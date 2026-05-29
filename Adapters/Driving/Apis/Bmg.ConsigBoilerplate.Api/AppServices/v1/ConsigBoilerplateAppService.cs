using Bmg.Project.Utils.Base;
using Bmg.Project.Utils.Data;
using Bmg.ConsigBoilerplate.Api.AppServices.v1.Interfaces;
using Bmg.ConsigBoilerplate.Api.Dtos.v1.ConsigBoilerplate;
using Bmg.ConsigBoilerplate.Domain.Models.v1;
using Bmg.ConsigBoilerplate.Domain.Services.v1;
using Microsoft.AspNetCore.JsonPatch;

namespace Bmg.ConsigBoilerplate.Api.AppServices.v1
{
    [BmgDynatraceTrace]
    public class ConsigBoilerplateAppService : BmgAppServiceBase<IConsigBoilerplateService>, IConsigBoilerplateAppService
    {       
        public async Task<IEnumerable<WeatherResponse>> GetAsync(CancellationToken cancellationToken)
        {
            var result = await Service.GetWeathersAsync(cancellationToken);

            return Mapper.Map<IEnumerable<WeatherResponse>>(result);
        }

        public async Task<PaginatedData<WeatherResponse>> GetPaginatedAsync(int pageSize, int currentPage, CancellationToken cancellationToken)
        {
            var result = await Service.GetWeathersPaginatedAsync(pageSize, currentPage, cancellationToken);

            return Mapper.Map<PaginatedData<WeatherResponse>>(result);
        }

        public async Task<WeatherResponse> GetAsync(long id, CancellationToken cancellationToken)
        {
            var result = await Service.GetWeatherAsync(id, cancellationToken);

            return Mapper.Map<WeatherResponse>(result);
        }

        public async Task<WeatherResponse> PostAsync(WeatherRequest request, CancellationToken cancellationToken)
        {
            var weather = Mapper.Map<WeatherModel>(request);

            var result = await Service.CreateWeatherAsync(weather, cancellationToken);

            return Mapper.Map<WeatherResponse>(result);
        }

        public async Task<bool> PatchAsync(long id, JsonPatchDocument<WeatherRequest> request, CancellationToken cancellationToken)
        {
            var weather = Mapper.Map<JsonPatchDocument<WeatherModel>>(request);

            var result = await Service.PatchWeatherAsync(id, weather, cancellationToken);

            return result;
        }

        public async Task<bool> PutAsync(WeatherRequest request, CancellationToken cancellationToken)
        {
            var weather = Mapper.Map<WeatherModel>(request);

            var result = await Service.UpdateWeatherAsync(weather, cancellationToken);

            return result;
        }

        public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken)
        {
            var result = await Service.DeleteWeatherAsync(id, cancellationToken);

            return result;
        }
    }
}
