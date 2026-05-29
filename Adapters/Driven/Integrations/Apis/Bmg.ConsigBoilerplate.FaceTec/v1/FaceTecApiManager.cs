using Bmg.Api.Client;
using Bmg.Api.Client.Base;
using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.FaceTec.v1;
using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.FaceTec.v1.Authentication;
using Bmg.Logging.Internal;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Bmg.Api.Client.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Bmg.Cache.Manager;

namespace Bmg.ConsigBoilerplate.FaceTec.v1
{
    public class FaceTecApiManager : ApiBase, IFaceTecApiManager
    {
        // TODO: AGRUPAR OS ENDPOINTS DA INTEGRAÇÃO
        public const string EndPointAuth = "api/v1/auth";
        public const string EndPointRecognition = "api/v1/recognition/facial";

        private const string CacheTokenKey = nameof(FaceTecApiManager);

        private readonly IBmgMemoryCacheManager _cacheManager; // TODO: ALTERAR PARA IBmgDistributedCacheManager
        private readonly IConfiguration _configuration;

        public FaceTecApiManager(
            IBmgApiClient apiClient,
            ILogger<FaceTecApiManager> logger,
            IConfiguration configuration,
            IBmgMemoryCacheManager cacheManager
        ) : base(configuration.GetValue<string>("Apis:FaceTec:PathUrl"), apiClient, logger)
        {
            _cacheManager = cacheManager;
            _configuration = configuration;
        }

        private async Task<AuthenticationResponse> GetTokenAsync(string cacheKey) // TODO: EXEMPLO DE AUTENTICAÇÃO EM API USANDO O REDIS COMO CACHE PARA COMPARTILHAR O TOKEN ENTRE OS PODS DA APLICAÇÃO
        {
            var tokenObject = await _cacheManager.GetOrCreateAsync(cacheKey, async cacheEntry =>
            {
                var token = await AuthenticateAsync(new AuthenticationRequest
                {
                    Username = _configuration.GetValue<string>("Credentials:FaceTec:UserName"),
                    Password = _configuration.GetValue<string>("Credentials:FaceTec:Passwords")
                });

                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(token.Expires);

                return token;
            });

            return tokenObject;
        }

        public async Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request)
        {
            try
            {
                var result = await ApiClient
                    .Url(EndPointAuth)
                    .WithBasicAuth(request.Username, request.Password)
                    .GetJsonAsync<AuthenticationResponse>(cancellationToken: CancellationToken);

                return result;
            }
            catch (FlurlHttpException ex)
            {
                await LogApiExceptionAsync(ex);

                throw;
            }
        }

        public async Task<bool> FacialRecognitionAsync(string cpf)
        {
            try
            {
                var tokenResponse = await GetTokenAsync(CacheTokenKey);

                var result = await ApiClient
                    .Url(EndPointRecognition, cpf)
                    .WithBmgHeaders<AuthenticationRequest, AuthenticationResponse>()
                    .WithOAuthBearerToken(tokenResponse.Token)
                    .PostAsync(cancellationToken: CancellationToken)
                    .ReceiveJson<bool>();

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
