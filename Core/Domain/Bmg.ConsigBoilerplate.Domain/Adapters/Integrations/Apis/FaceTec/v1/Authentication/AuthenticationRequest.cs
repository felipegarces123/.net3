using Bmg.Logging.Internal.Attributes;

namespace Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.FaceTec.v1.Authentication
{
    public record AuthenticationRequest
    {
        public string Username { get; init; }
        
        [BmgSensitiveData]
        public string Password { get; init; }
    }
}
