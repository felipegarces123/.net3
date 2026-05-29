using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bmg.ConsigBoilerplate.Domain.Models.v1
{
    public record WeatherModel
    {
        public long Id { get; init; }
        public DateTime Date { get; init; }
        public int TemperatureC { get; init; }
        public string? Summary { get; init; }
    }
}
