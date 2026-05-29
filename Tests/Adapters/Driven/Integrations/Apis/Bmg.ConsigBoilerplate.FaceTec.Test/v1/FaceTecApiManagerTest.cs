using AutoFixture;
using Bmg.Api.Client;
using Bmg.Cache.Manager;
using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.FaceTec.v1.Authentication;
using Bmg.ConsigBoilerplate.FaceTec.v1;
using Bmg.Logging.Internal;
using Flurl.Http;
using Flurl.Http.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.AutoMock;
using Org.BouncyCastle.Asn1.Ocsp;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bmg.ConsigBoilerplate.FaceTec.Test.v1
{
    [ExcludeFromCodeCoverage]
    public class FaceTecApiManagerTest
    {
        private const string TestUrl = "https://facetec.com";

        private readonly AutoMocker _mocker;
        private readonly Dictionary<string, string> InMemorySettings;

        public FaceTecApiManagerTest()
        {
            _mocker = new AutoMocker();

            InMemorySettings = new Dictionary<string, string>
            {
                {"Apis:FaceTec:PathUrl", TestUrl},
            };
        }

        [Fact]
        public async Task Authenticate_ReturnOkAsync()
        {
            using (var httpTest = new HttpTest())
            {
                // TODO: ALTERAR PARA IBmgDistributedCacheManager
                var cacheManagerMock = _mocker.GetMock<IBmgMemoryCacheManager>(); // TODO: EXEMPLO DE USO DO AUTOMOCKER NOS TESTES

                var bmgLoggingMock = _mocker.GetMock<ILogger<FaceTecApiManager>>();

                var bmgApiClientMock = _mocker.GetMock<IBmgApiClient>();
                bmgApiClientMock
                .Setup(data => data.Url(FaceTecApiManager.EndPointAuth))
                .Returns(TestUrl.WithSettings(settings => { }));

                var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(InMemorySettings)
                .Build();

                httpTest
                    .RespondWithJson(
                        new Fixture().Build<AuthenticationResponse>().Create(),
                        (int)HttpStatusCode.OK);

                var manager = new FaceTecApiManager(
                    bmgApiClientMock.Object,
                    bmgLoggingMock.Object,
                    configuration,
                    cacheManagerMock.Object
                );

                //Act
                var result = await manager.AuthenticateAsync(new AuthenticationRequest
                {
                    Username = "user",
                    Password = "pass"
                });

                //ShouldBe
                result.ShouldBeOfType<AuthenticationResponse>();

                result.ShouldNotBeNull();
            }
        }

        [Fact]
        public async Task Authenticate_ReturnUnauthorizedAsync()
        {
            using (var httpTest = new HttpTest())
            {
                // TODO: ALTERAR PARA IBmgDistributedCacheManager
                var cacheManagerMock = new Mock<IBmgMemoryCacheManager>(); 

                var bmgLoggingMock = new Mock<ILogger<FaceTecApiManager>>();

                var bmgApiClientMock = new Mock<IBmgApiClient>();
                bmgApiClientMock
                .Setup(data => data.Url(FaceTecApiManager.EndPointAuth))
                .Returns(TestUrl.WithSettings(settings => { }));

                var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(InMemorySettings)
                .Build();

                var testUnauthorized = "Unauthorized";

                httpTest
                    .RespondWith(
                        testUnauthorized,
                        (int)HttpStatusCode.Unauthorized);

                var manager = new FaceTecApiManager(
                    bmgApiClientMock.Object,
                    bmgLoggingMock.Object,
                    configuration,
                    cacheManagerMock.Object
                );

                //Act
                var exception = await Should.ThrowAsync<FlurlHttpException>(() => manager.AuthenticateAsync(new AuthenticationRequest
                {
                    Username = "user",
                    Password = "pass"
                }));

                var response = await exception.GetResponseStringAsync();

                //ShouldBe
                response.ShouldBe(testUnauthorized);
            }
        }
    }
}
