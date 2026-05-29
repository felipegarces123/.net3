using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.Bmg.Metabusca.v1.ReceitaFederal;

namespace Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.Bmg.Metabusca.v1
{
    public interface IMetabuscaApiManager
    {
        void SetCancellationToken(CancellationToken cancellationToken);

        Task<ReceitaFederalResponse> ValidaCpfAsync(string cpf);
    }
}
