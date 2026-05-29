using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.FaceTec.v1.Authentication;

namespace Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.FaceTec.v1
{
    public interface IFaceTecApiManager
    {
        void SetCancellationToken(CancellationToken cancellationToken);

        Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request);
    }
}
