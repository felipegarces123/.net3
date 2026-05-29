using Bmg.Api.Client;
using Bmg.Api.Client.Base;
using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.Bmg.Metabusca.v1;
using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.Bmg.Metabusca.v1.ReceitaFederal;
using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.FaceTec.v1.Authentication;
using Bmg.Logging.Internal;
using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Bmg.Api.Client.Extensions;

namespace Bmg.ConsigBoilerplate.Metabusca.v1
{
    public class MetabuscaApiManager : ApiBase, IMetabuscaApiManager
    {
        // TODO: AGRUPAR OS ENDPOINTS DA INTEGRAÇÃO
        public const string EndPointCliente = "api/v1/cliente";       

        public MetabuscaApiManager(IBmgApiClient apiClient, ILogger<MetabuscaApiManager> logger, IConfiguration configuration) :
            base(configuration.GetValue<string>("Apis:Metabusca:PathUrl"), apiClient, logger)
        { }

        public async Task<ReceitaFederalResponse> ValidaCpfAsync(string cpf)
        {
            try
            {
                var result = await ApiClient
                    .Url(Url.Combine(EndPointCliente, cpf, "validar"))
                    .WithBmgHeaders<ReceitaFederalResponse>(HeaderType.Response)
                    .PostAsync(cancellationToken: CancellationToken)
                    .ReceiveJson<ReceitaFederalResponse>();

                return result;
            }
            catch (FlurlHttpException ex)
            {
                await LogApiExceptionAsync(ex);

                throw;
            }
        }
    }
}
