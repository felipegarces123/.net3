using AutoFixture;
using Bmg.Connection.Manager.Data;
using Bmg.ConsigBoilerplate.Api.AppServices.v1.Interfaces;
using Bmg.ConsigBoilerplate.Api.Controllers.v1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json.Serialization;
using Shouldly;
using System.Diagnostics.CodeAnalysis;
using Bmg.ConsigBoilerplate.Api.Dtos.v1.ConsigBoilerplate;
using Bmg.Project.Utils.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Bmg.Project.Utils.Services;
using Bmg.Project.Utils.Interfaces;

namespace Bmg.ConsigBoilerplate.Api.Test.v1
{
    [ExcludeFromCodeCoverage]
    public class ConsigBoilerplateControllerTest
    {
        private static (ConsigBoilerplateController controller, ServiceProvider provider) CreateController(Mock<IConsigBoilerplateAppService> appServiceMock)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IConsigBoilerplateAppService>(appServiceMock.Object);
            // Register notifier mock required by BmgControllerBase
            var notifierMock = new Mock<IBmgNotifier>();
            notifierMock.Setup(n => n.HasNotifications()).Returns(false);
            notifierMock.Setup(n => n.GetNotifications()).Returns(Array.Empty<Bmg.Project.Utils.Notifications.BmgNotification>());
            services.AddSingleton<IBmgNotifier>(notifierMock.Object);

            var provider = services.BuildServiceProvider();
            BmgServiceProviderLocator.MockTest(provider);

            var controller = new ConsigBoilerplateController();
            controller.ControllerContext ??= new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                RequestServices = provider
            };

            return (controller, (ServiceProvider)provider);
        }

        [Fact]
        public async Task ValidateWeatherResponseAsync()
        {
            await Task.Run(() =>
            {
                var weather = new WeatherResponse { TemperatureC = 10 };
                weather.TemperatureF.ShouldBe(32 + (int)(weather.TemperatureC / 0.5556));
            });
        }

        [Fact]
        public async Task GetAllWeathers_ReturnOkAsync()
        {
            var appServiceMock = new Mock<IConsigBoilerplateAppService>();
            appServiceMock
                .Setup(data => data.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Fixture().CreateMany<WeatherResponse>(1).AsEnumerable());

            var (controller, _) = CreateController(appServiceMock);

            var result = await controller.GetAsync(It.IsAny<CancellationToken>());

            var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
            var weathers = okResult.Value.ShouldBeAssignableTo<IEnumerable<WeatherResponse>>();
            weathers.ShouldHaveSingleItem();
            appServiceMock.Verify();
        }

        [Fact]
        public async Task GetAllWeathers_ReturnNoContentAsync()
        {
            var appServiceMock = new Mock<IConsigBoilerplateAppService>();
            appServiceMock
                .Setup(data => data.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<WeatherResponse>());

            var (controller, _) = CreateController(appServiceMock);

            var result = await controller.GetAsync(It.IsAny<CancellationToken>());

            result.Result.ShouldBeOfType<NoContentResult>();
            appServiceMock.Verify();
        }

        [Fact]
        public async Task GetWeatherById_ReturnOkAsync()
        {
            var testId = 1;

            var appServiceMock = new Mock<IConsigBoilerplateAppService>();
            appServiceMock
                .Setup(data => data.GetAsync(testId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Fixture().Build<WeatherResponse>().With(w => w.Id, testId).Create());

            var (controller, _) = CreateController(appServiceMock);

            var result = await controller.GetAsync(testId, It.IsAny<CancellationToken>());

            var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
            var weather = okResult.Value.ShouldBeOfType<WeatherResponse>();
            weather.Id.ShouldBe(testId);
            appServiceMock.Verify();
        }

        [Fact]
        public async Task GetWeatherById_ReturnNoContentAsync()
        {
            var testId = 1;
            var appServiceMock = new Mock<IConsigBoilerplateAppService>();
            appServiceMock
                .Setup(data => data.GetAsync(testId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((WeatherResponse?)null);

            var (controller, _) = CreateController(appServiceMock);

            var result = await controller.GetAsync(testId, It.IsAny<CancellationToken>());

            result.Result.ShouldBeOfType<NoContentResult>();
            appServiceMock.Verify();
        }

        [Fact]
        public async Task GetWeathersPaginated_ReturnOkAsync()
        {
            var testPageSize = 10;
            var testCurrentPage = 1;

            var appServiceMock = new Mock<IConsigBoilerplateAppService>();
            appServiceMock
                .Setup(data => data.GetPaginatedAsync(testPageSize, testCurrentPage, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var result = new PaginatedData<WeatherResponse>();
                    result.Data = new Fixture().CreateMany<WeatherResponse>(testPageSize).AsEnumerable();
                    result.CurrentPage = testCurrentPage;
                    result.PageSize = testPageSize;
                    result.TotalCount = result.Data.Count();
                    return result;
                });

            var (controller, provider) = CreateController(appServiceMock);

            controller.ControllerContext.HttpContext = new DefaultHttpContext { RequestServices = provider };

            var result = await controller.GetPaginatedAsync(testPageSize, testCurrentPage, It.IsAny<CancellationToken>());

            var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
            var weathers = okResult.Value.ShouldBeAssignableTo<IEnumerable<WeatherResponse>>();
            weathers?.Count().ShouldBe(testPageSize);

            controller.Response.Headers.ShouldContainKeyAndValue(
                "X-Pagination",
                $"{{\"totalCount\":{testPageSize},\"pageSize\":{testPageSize},\"currentPage\":{testCurrentPage},\"totalPages\":1,\"hasNext\":false,\"hasPrevious\":false}}"
            );

            appServiceMock.Verify();
        }

        [Fact]
        public async Task PostWeather_ReturnCreatedAtRouteAsync()
        {
            var testId = 1L;
            var testSummary = "Teste";
            var testWeather = new Fixture().Build<WeatherRequest>().With(w => w.Summary, testSummary).Create();

            var appServiceMock = new Mock<IConsigBoilerplateAppService>();
            appServiceMock
                .Setup(data => data.PostAsync(testWeather, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Fixture().Build<WeatherResponse>().With(w => w.Id, testId).With(w => w.Summary, testSummary).Create());

            var (controller, _) = CreateController(appServiceMock);

            var result = await controller.PostAsync(testWeather, It.IsAny<CancellationToken>());

            var createdAtRouteResult = result.Result.ShouldBeOfType<CreatedAtRouteResult>();
            var weather = createdAtRouteResult.Value.ShouldBeOfType<WeatherResponse>();
            weather.Id.ShouldBe(testId);
            weather.Summary.ShouldBe(testSummary);
            createdAtRouteResult.RouteValues?.ShouldContainKeyAndValue("id", testId);
            appServiceMock.Verify();
        }

        [Fact]
        public async Task PatchWeather_ReturnNoContentAsync()
        {
            var testId = 1;

            var testWeatherPatch = new JsonPatchDocument<WeatherRequest>(
                new List<Operation<WeatherRequest>>
                {
                    new Operation<WeatherRequest>
                    {
                        op = "replace",
                        path = "/summary",
                        value = "Teste"
                    }
                },
                new DefaultContractResolver()
            );

            var appServiceMock = new Mock<IConsigBoilerplateAppService>();
            appServiceMock
                .Setup(data => data.PatchAsync(testId, testWeatherPatch, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var (controller, _) = CreateController(appServiceMock);

            var result = await controller.PatchAsync(testId, testWeatherPatch, It.IsAny<CancellationToken>());

            result.ShouldBeOfType<NoContentResult>();
            appServiceMock.Verify();
        }

        [Fact]
        public async Task PatchWeather_ReturnBadRequestAsync()
        {
            var testId = 1;

            var testWeatherPatch = new JsonPatchDocument<WeatherRequest>(
                new List<Operation<WeatherRequest>>
                {
                    new Operation<WeatherRequest>
                    {
                        op = "replace",
                        path = "/summary",
                        value = "Teste"
                    }
                },
                new DefaultContractResolver()
            );

            var appServiceMock = new Mock<IConsigBoilerplateAppService>();
            appServiceMock
                .Setup(data => data.PatchAsync(testId, testWeatherPatch, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var (controller, _) = CreateController(appServiceMock);

            var result = await controller.PatchAsync(testId, testWeatherPatch, It.IsAny<CancellationToken>());

            result.ShouldBeOfType<BadRequestResult>();
            appServiceMock.Verify();
        }

        [Fact]
        public async Task PutWeather_ReturnNoContentAsync()
        {
            var testWeather = new Fixture().Build<WeatherRequest>().Create();

            var appServiceMock = new Mock<IConsigBoilerplateAppService>();
            appServiceMock
                .Setup(data => data.PutAsync(testWeather, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var (controller, _) = CreateController(appServiceMock);

            var result = await controller.PutAsync(testWeather, It.IsAny<CancellationToken>());

            result.ShouldBeOfType<NoContentResult>();
            appServiceMock.Verify();
        }

        [Fact]
        public async Task PutWeather_ReturnBadRequestAsync()
        {
            var testWeather = new Fixture().Build<WeatherRequest>().Create();

            var appServiceMock = new Mock<IConsigBoilerplateAppService>();
            appServiceMock
                .Setup(data => data.PutAsync(testWeather, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var (controller, _) = CreateController(appServiceMock);

            var result = await controller.PutAsync(testWeather, It.IsAny<CancellationToken>());

            result.ShouldBeOfType<BadRequestResult>();
            appServiceMock.Verify();
        }

        [Fact]
        public async Task DeleteWeather_ReturnNoContentAsync()
        {
            var testId = 1;

            var appServiceMock = new Mock<IConsigBoilerplateAppService>();
            appServiceMock
                .Setup(data => data.DeleteAsync(testId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var (controller, _) = CreateController(appServiceMock);

            var result = await controller.DeleteAsync(testId, It.IsAny<CancellationToken>());

            result.ShouldBeOfType<NoContentResult>();
            appServiceMock.Verify();
        }

        [Fact]
        public async Task DeleteWeather_ReturnBadRequestAsync()
        {
            var testId = 1;

            var appServiceMock = new Mock<IConsigBoilerplateAppService>();
            appServiceMock
                .Setup(data => data.DeleteAsync(testId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var (controller, _) = CreateController(appServiceMock);

            var result = await controller.DeleteAsync(testId, It.IsAny<CancellationToken>());

            result.ShouldBeOfType<BadRequestResult>();
            appServiceMock.Verify();
        }
    }
}