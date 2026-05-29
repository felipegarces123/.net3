using AutoFixture;
using Bmg.Api.Client;
using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.Bmg.Metabusca.v1.ReceitaFederal;
using Bmg.ConsigBoilerplate.Metabusca.v1;
using Bmg.Logging.Internal;
using Flurl;
using Flurl.Http;
using Flurl.Http.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
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

namespace Bmg.ConsigBoilerplate.Metabusca.Test.v1
{
    [ExcludeFromCodeCoverage]
    public class MetabuscaApiManagerTest
    {
        private const string TestUrl = "https://metabusca.bancobmg.com.br";

        private readonly Dictionary<string, string> InMemorySettings;

        public MetabuscaApiManagerTest()
        {
            InMemorySettings = new Dictionary<string, string>
            {
                {"Apis:Metabusca:PathUrl", TestUrl},
            };
        }


        [Fact]
        public async Task ValidaCpf_ReturnOkAsync()
        {
            using (var httpTest = new HttpTest())
            {
                var cpf = It.IsAny<string>();

                var bmgLoggingMock = new Mock<ILogger<MetabuscaApiManager>>();

                var bmgApiClientMock = new Mock<IBmgApiClient>();
                bmgApiClientMock
                .Setup(data => data.Url(Url.Combine(MetabuscaApiManager.EndPointCliente, cpf, "validar")))
                .Returns(TestUrl.WithSettings(settings => { }));

                var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(InMemorySettings)
                .Build();

                httpTest
                    .RespondWithJson(
                        new Fixture().Build<ReceitaFederalResponse>().Create(),
                        (int)HttpStatusCode.OK);

                var manager = new MetabuscaApiManager(
                    bmgApiClientMock.Object,
                    bmgLoggingMock.Object,
                    configuration
                );

                //Act
                var result = await manager.ValidaCpfAsync(It.IsAny<string>());

                //ShouldBe
                result.ShouldBeOfType<ReceitaFederalResponse>();

                result.ShouldNotBeNull();
            }
        }

        [Fact]
        public async Task ValidaCpf_ReturnUnauthorizedAsync()
        {
            using (var httpTest = new HttpTest())
            {
                var cpf = It.IsAny<string>();

                var bmgLoggingMock = new Mock<ILogger<MetabuscaApiManager>>();

                var bmgApiClientMock = new Mock<IBmgApiClient>();
                bmgApiClientMock
                .Setup(data => data.Url(Url.Combine(MetabuscaApiManager.EndPointCliente, cpf, "validar")))
                .Returns(TestUrl.WithSettings(settings => { }));

                var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(InMemorySettings)
                .Build();

                var testUnauthorized = "Unauthorized";

                httpTest
                    .RespondWith(
                        testUnauthorized,
                        (int)HttpStatusCode.Unauthorized);

                var manager = new MetabuscaApiManager(
                    bmgApiClientMock.Object,
                    bmgLoggingMock.Object,
                    configuration
                );

                //Act
                var exception = await Should.ThrowAsync<FlurlHttpException>(() => manager.ValidaCpfAsync(It.IsAny<string>()));

                var response = await exception.GetResponseStringAsync();

                //ShouldBe
                response.ShouldBe(testUnauthorized);
            }
        }
    }
}
