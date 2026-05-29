using AutoMapper;
using Bmg.Project.Utils.Data;
using Bmg.ConsigBoilerplate.Database.Entities.v1;
using Bmg.ConsigBoilerplate.Domain.Models.v1;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Bmg.ConsigBoilerplate.Application.Mappings.v1
{
    [ExcludeFromCodeCoverage]
    public class ModelMappingProfile : Profile
    {
        public ModelMappingProfile()
        {
            CreateMap<WeatherModel, Weather>().ReverseMap();
            CreateMap<PaginatedData<WeatherModel>, PaginatedData<Weather>>().ReverseMap();
            CreateMap<Operation<WeatherModel>, Operation<Weather>>().ReverseMap();
            CreateMap<JsonPatchDocument<WeatherModel>, JsonPatchDocument<Weather>>().ReverseMap();
        }
    }
}
