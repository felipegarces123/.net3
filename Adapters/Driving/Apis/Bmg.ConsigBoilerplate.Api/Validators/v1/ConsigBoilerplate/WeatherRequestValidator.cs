using Bmg.Logging.Internal.Attributes;
using FluentValidation;
using System.ComponentModel.DataAnnotations;
using Bmg.ConsigBoilerplate.Api.Dtos.v1.ConsigBoilerplate;

namespace Bmg.ConsigBoilerplate.Api.Validators.v1.ConsigBoilerplate
{
    // TODO: NÃO UTILIZE INJEÇÃO DE DEPENDÊNCIA TRANSIENT/SCOPED NAS CLASSES DE VALIDAÇÃO POIS ELAS SÃO SINGLETON
    public class WeatherRequestValidator : AbstractValidator<WeatherRequest>
    {
        public WeatherRequestValidator()
        {
            RuleFor(request => request.Date)
                .NotNull();

            RuleFor(request => request.Summary)
                .NotNull();
        }
    }
}
