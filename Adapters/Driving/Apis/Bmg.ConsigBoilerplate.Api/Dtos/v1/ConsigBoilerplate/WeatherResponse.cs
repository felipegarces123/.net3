using Bmg.Logging.Internal.Attributes;

namespace Bmg.ConsigBoilerplate.Api.Dtos.v1.ConsigBoilerplate
{
    public record WeatherResponse
    {
        [BmgSensitiveData] // TODO: EXEMPLO DE OCULTAÇÃO DE DADO SENSÍVEL NO LOG
        public long Id { get; init; }

        public DateTime Date { get; init; }

        [BmgIgnoredField] // TODO: EXEMPLO DE CAMPO A SER IGNORADO NO LOG, COMO POR EXEMPLO UM CAMPO BASE64
        public int TemperatureC { get; init; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; init; }
    }
}
