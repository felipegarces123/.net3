using AutoMapper;
using Bmg.Connection.Manager.Data;
using Bmg.Project.Utils.Data;
using Bmg.ConsigBoilerplate.Api.Dtos.v1.ConsigBoilerplate;
using Bmg.ConsigBoilerplate.Domain.Models.v1;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using System.Diagnostics.CodeAnalysis;

namespace Bmg.ConsigBoilerplate.Api.Mappings.v1
{
    [ExcludeFromCodeCoverage]
    public class DtoMappingProfile : Profile
    {
        public DtoMappingProfile()
        {
            CreateMap<WeatherModel, WeatherResponse>().ReverseMap();
            CreateMap<PaginatedData<WeatherModel>, PaginatedData<WeatherResponse>>().ReverseMap();
            CreateMap<WeatherRequest, WeatherModel>().ReverseMap();
            CreateMap<Operation<WeatherRequest>, Operation<WeatherModel>>().ReverseMap();
            CreateMap<JsonPatchDocument<WeatherRequest>, JsonPatchDocument<WeatherModel>>().ReverseMap();
        }
    }
}
