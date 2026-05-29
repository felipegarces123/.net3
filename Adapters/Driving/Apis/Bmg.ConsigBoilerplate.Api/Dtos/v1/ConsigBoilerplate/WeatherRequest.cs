using Bmg.Logging.Internal.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Bmg.ConsigBoilerplate.Api.Dtos.v1.ConsigBoilerplate
{
    public record WeatherRequest
    {
        public long Id { get; init; }

        public DateTime? Date { get; init; }

        public int TemperatureC { get; init; }

        public string? Summary { get; init; }
    }
}
