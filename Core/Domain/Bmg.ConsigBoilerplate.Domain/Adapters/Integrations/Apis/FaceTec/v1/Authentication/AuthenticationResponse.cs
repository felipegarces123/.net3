using Bmg.Logging.Internal.Attributes;

namespace Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.FaceTec.v1.Authentication
{
    public record AuthenticationResponse
    {
        [BmgSensitiveData]
        public string Token { get; init; }
        public double Expires { get; set; } 
    }
}
