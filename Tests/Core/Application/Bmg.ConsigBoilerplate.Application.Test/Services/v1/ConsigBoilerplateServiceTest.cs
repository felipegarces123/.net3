using AutoFixture;
using Bmg.Connection.Manager.Data;
using Bmg.ConsigBoilerplate.Application.Services.v1;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.JsonPatch;
using Moq;
using Newtonsoft.Json.Serialization;
using Shouldly;
using System.Diagnostics.CodeAnalysis;
using Bmg.ConsigBoilerplate.Domain.Models.v1;
using Bmg.ConsigBoilerplate.Database.UnitOfWork.Interfaces.v1;
using Bmg.ConsigBoilerplate.Database.Entities.v1;
using Bmg.Project.Utils.Data;
using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Apis.FaceTec.v1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Bmg.Project.Utils.Services;
using AutoMapper;
using Bmg.Project.Utils.Interfaces;

namespace Bmg.ConsigBoilerplate.Application.Test.Services.v1
{
    [ExcludeFromCodeCoverage]
    public class ConsigBoilerplateServiceTest
    {
        private static ServiceProvider EnsureProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Mock Notifier
            var notifierMock = new Mock<IBmgNotifier>();
            notifierMock.Setup(n => n.HasNotifications()).Returns(false);
            notifierMock.Setup(n => n.IsValid()).Returns(true);
            notifierMock.Setup(n => n.GetNotifications()).Returns(Array.Empty<Bmg.Project.Utils.Notifications.BmgNotification>());
            services.AddSingleton<IBmgNotifier>(notifierMock.Object);

            // Mock AutoMapper
            var mapperMock = new Mock<IMapper>();
            mapperMock.Setup(m => m.Map<IEnumerable<WeatherModel>>(It.IsAny<IEnumerable<Weather>>()))
                      .Returns((IEnumerable<Weather> src) => src.Select(w => new WeatherModel { Id = w.Id, Date = w.Date, TemperatureC = w.TemperatureC, Summary = w.Summary }));
            mapperMock.Setup(m => m.Map<WeatherModel>(It.IsAny<Weather>()))
                      .Returns((Weather w) => w == null ? null : new WeatherModel { Id = w.Id, Date = w.Date, TemperatureC = w.TemperatureC, Summary = w.Summary });
            mapperMock.Setup(m => m.Map<Weather>(It.IsAny<WeatherModel>()))
                      .Returns((WeatherModel wm) => wm == null ? null : new Weather { Id = wm.Id, Date = wm.Date, TemperatureC = wm.TemperatureC, Summary = wm.Summary });
            mapperMock.Setup(m => m.Map<PaginatedData<WeatherModel>>(It.IsAny<PaginatedData<Weather>>()))
                      .Returns((PaginatedData<Weather> p) => new PaginatedData<WeatherModel>
                      {
                          Data = p.Data.Select(w => new WeatherModel { Id = w.Id, Date = w.Date, TemperatureC = w.TemperatureC, Summary = w.Summary }),
                          CurrentPage = p.CurrentPage,
                          PageSize = p.PageSize,
                          TotalCount = p.TotalCount
                      });
            services.AddSingleton<IMapper>(mapperMock.Object);

            var provider = services.BuildServiceProvider();
            BmgServiceProviderLocator.MockTest(provider);
            return (ServiceProvider)provider;
        }

        [Fact]
        public async Task GetWeathers_ReturnListWeatherAsync()
        {
            EnsureProvider();

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            unitOfWorkMock
                .Setup(data => data.Weathers.SelectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IEnumerable<Weather>>(new Fixture().CreateMany<Weather>(1).AsEnumerable()));

            var faceTecMock = new Mock<IFaceTecApiManager>();

            var service = new ConsigBoilerplateService(
                unitOfWorkMock.Object,
                faceTecMock.Object
            );

            // Act
            var result = await service.GetWeathersAsync(It.IsAny<CancellationToken>());

            // ShouldBe
            var list = result.ShouldBeAssignableTo<IEnumerable<WeatherModel>>();

            list.ShouldHaveSingleItem();

            unitOfWorkMock.Verify();
        }

        [Fact]
        public async Task GetWeathers_ReturnEmptyListAsync()
        {
            EnsureProvider();

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            unitOfWorkMock
                .Setup(data => data.Weathers.SelectAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IEnumerable<Weather>>(new Fixture().CreateMany<Weather>(0).AsEnumerable()));

            var faceTecMock = new Mock<IFaceTecApiManager>();

            var service = new ConsigBoilerplateService(
                unitOfWorkMock.Object,
                faceTecMock.Object
            );

            // Act
            var result = await service.GetWeathersAsync(It.IsAny<CancellationToken>());

            // ShouldBe
            var list = result.ShouldBeAssignableTo<IEnumerable<WeatherModel>>();

            list.ShouldBeEmpty();

            unitOfWorkMock.Verify();
        }

        [Fact]
        public async Task GetWeathersPaginated_ReturnListPaginatedWeatherAsync()
        {
            EnsureProvider();

            var testPageSize = 10;
            var testPageNumber = 1;

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            unitOfWorkMock
                .Setup(data => data.Weathers.SelectPaginationAsync(testPageSize, testPageNumber, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var result = new PaginatedData<Weather>();

                    result.Data = new Fixture().CreateMany<Weather>(testPageSize).AsEnumerable();
                    result.CurrentPage = testPageNumber;
                    result.PageSize = testPageSize;
                    result.TotalCount = result.Data.Count();

                    return result;
                });

            var faceTecMock = new Mock<IFaceTecApiManager>();
            var service = new ConsigBoilerplateService(unitOfWorkMock.Object, faceTecMock.Object);

            // Act
            var result = await service.GetWeathersPaginatedAsync(testPageSize, testPageNumber, It.IsAny<CancellationToken>());

            // ShouldBe
            var list = result.Data.ShouldBeAssignableTo<IEnumerable<WeatherModel>>();

            list?.Count().ShouldBe(testPageSize);

            result.PageSize.ShouldBe(testPageSize);
            result.CurrentPage.ShouldBe(testPageNumber);
            result.TotalCount.ShouldBe(testPageSize);

            unitOfWorkMock.Verify();
        }

        [Fact]
        public async Task GetWeatherById_ReturnWeatherAsync()
        {
            EnsureProvider();

            var testId = 1;

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            unitOfWorkMock
                .Setup(data => data.Weathers.SelectAsync(It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
                .Returns(Task.FromResult<Weather?>(new Fixture().Build<Weather>().With(w => w.Id, testId).Create()));

            var faceTecMock = new Mock<IFaceTecApiManager>();
            var service = new ConsigBoilerplateService(unitOfWorkMock.Object, faceTecMock.Object);

            // Act
            var weather = await service.GetWeatherAsync(testId, It.IsAny<CancellationToken>());

            // ShouldBe
            weather.ShouldBeOfType<WeatherModel>();

            weather.Id.ShouldBe(testId);

            unitOfWorkMock.Verify();
        }

        [Fact]
        public async Task GetWeatherById_ReturnNullAsync()
        {
            EnsureProvider();

            var testId = 1;

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            unitOfWorkMock
                .Setup(data => data.Weathers.SelectAsync(It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
                .Returns(Task.FromResult<Weather?>(null));

            var faceTecMock = new Mock<IFaceTecApiManager>();
            var service = new ConsigBoilerplateService(unitOfWorkMock.Object, faceTecMock.Object);

            // Act
            var weather = await service.GetWeatherAsync(testId, It.IsAny<CancellationToken>());

            // ShouldBe
            weather.ShouldBeNull();

            unitOfWorkMock.Verify();
        }

        [Fact]
        public async Task CreateWeather_ReturnIdAsync()
        {
            EnsureProvider();

            var testId = 1;

            var testWeather = new Fixture().Build<WeatherModel>().With(w => w.Id, testId).Create();

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            var faceTecMock = new Mock<IFaceTecApiManager>();
            var service = new ConsigBoilerplateService(unitOfWorkMock.Object, faceTecMock.Object);

            // Act
            var weather = await service.CreateWeatherAsync(testWeather, It.IsAny<CancellationToken>());

            // ShouldBe
            weather.ShouldBeOfType<WeatherModel>();

            weather.Id.ShouldBe(testId);

            unitOfWorkMock.Verify();
        }

        [Fact]
        public async Task PatchWeather_ReturnTrueAsync()
        {
            EnsureProvider();

            var testId = 1;

            var testWeatherPatch = new JsonPatchDocument<WeatherModel>(
                new List<Operation<WeatherModel>>
                {
                    new Operation<WeatherModel>
                    {
                        op = "replace",
                        path = "/summary",
                        value = "Teste"
                    }
                },
                new DefaultContractResolver()
            );

            var testWeather = new Fixture().Build<Weather>().With(w => w.Id, testId).Create();

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            unitOfWorkMock
                .Setup(data => data.Weathers.UpdateAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            unitOfWorkMock
                .Setup(data => data.Weathers.SelectAsync(It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
                .Returns(Task.FromResult<Weather?>(testWeather));

            var faceTecMock = new Mock<IFaceTecApiManager>();
            var service = new ConsigBoilerplateService(unitOfWorkMock.Object, faceTecMock.Object);

            // Act
            var result = await service.PatchWeatherAsync(testId, testWeatherPatch, It.IsAny<CancellationToken>());

            // ShouldBe
            result.ShouldBeTrue();

            unitOfWorkMock.Verify();
        }

        [Fact]
        public async Task PatchWeather_ReturnFalseAsync()
        {
            EnsureProvider();

            var testId = 1;

            var testWeatherPatch = new JsonPatchDocument<WeatherModel>(
                new List<Operation<WeatherModel>>
                {
                    new Operation<WeatherModel>
                    {
                        op = "replace",
                        path = "/summary",
                        value = "Teste"
                    }
                },
                new DefaultContractResolver()
            );

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            unitOfWorkMock
                .Setup(data => data.Weathers.UpdateAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var faceTecMock = new Mock<IFaceTecApiManager>();
            var service = new ConsigBoilerplateService(unitOfWorkMock.Object, faceTecMock.Object);

            // Act
            var result = await service.PatchWeatherAsync(testId, testWeatherPatch, It.IsAny<CancellationToken>());

            // ShouldBe
            result.ShouldBeFalse();

            unitOfWorkMock.Verify();
        }

        [Fact]
        public async Task UpdateWeather_ReturnTrueAsync()
        {
            EnsureProvider();

            var testId = 1;

            var testWeather = new Fixture().Build<WeatherModel>().With(w => w.Id, testId).Create();
            var testWeatherEntity = new Fixture().Build<Weather>().With(w => w.Id, testId).Create();

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            unitOfWorkMock
                .Setup(data => data.Weathers.UpdateAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            unitOfWorkMock
                .Setup(data => data.Weathers.SelectAsync(It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
                .Returns(Task.FromResult<Weather?>(testWeatherEntity));

            var faceTecMock = new Mock<IFaceTecApiManager>();
            var service = new ConsigBoilerplateService(unitOfWorkMock.Object, faceTecMock.Object);

            // Act
            var result = await service.UpdateWeatherAsync(testWeather, It.IsAny<CancellationToken>());

            // ShouldBe
            result.ShouldBeTrue();

            unitOfWorkMock.Verify();
        }

        [Fact]
        public async Task UpdateWeather_ReturnFalseAsync()
        {
            EnsureProvider();

            var testId = 1;

            var testWeather = new Fixture().Build<WeatherModel>().With(w => w.Id, testId).Create();
            var testWeatherEntity = new Fixture().Build<Weather>().With(w => w.Id, testId).Create();

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            unitOfWorkMock
                .Setup(data => data.Weathers.UpdateAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            unitOfWorkMock
                .Setup(data => data.Weathers.SelectAsync(It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
                .Returns(Task.FromResult<Weather?>(testWeatherEntity));

            var faceTecMock = new Mock<IFaceTecApiManager>();
            var service = new ConsigBoilerplateService(unitOfWorkMock.Object, faceTecMock.Object);

            // Act
            var result = await service.UpdateWeatherAsync(testWeather, It.IsAny<CancellationToken>());

            // ShouldBe
            result.ShouldBeFalse();

            unitOfWorkMock.Verify();
        }

        [Fact]
        public async Task UpdateWeather_ReturnIdFalseAsync()
        {
            EnsureProvider();

            var testId = 1;

            var testWeather = new Fixture().Build<WeatherModel>().With(w => w.Id, testId).Create();

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            unitOfWorkMock
                .Setup(data => data.Weathers.SelectAsync(It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
                .Returns(Task.FromResult<Weather?>(null));

            var faceTecMock = new Mock<IFaceTecApiManager>();
            var service = new ConsigBoilerplateService(unitOfWorkMock.Object, faceTecMock.Object);

            // Act
            var result = await service.UpdateWeatherAsync(testWeather, It.IsAny<CancellationToken>());

            // ShouldBe
            result.ShouldBeFalse();

            unitOfWorkMock.Verify();
        }

        [Fact]
        public async Task DeleteWeather_ReturnTrueAsync()
        {
            EnsureProvider();

            var testId = 1;

            var testWeather = new Fixture().Build<Weather>().With(w => w.Id, testId).Create();

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            unitOfWorkMock
                .Setup(data => data.Weathers.DeleteAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            unitOfWorkMock
                .Setup(data => data.Weathers.SelectAsync(It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
                .Returns(Task.FromResult<Weather?>(testWeather));

            var faceTecMock = new Mock<IFaceTecApiManager>();
            var service = new ConsigBoilerplateService(unitOfWorkMock.Object, faceTecMock.Object);

            // Act
            var result = await service.DeleteWeatherAsync(testId, It.IsAny<CancellationToken>());

            // ShouldBe
            result.ShouldBeTrue();

            unitOfWorkMock.Verify();
        }

        [Fact]
        public async Task DeleteWeather_ReturnFalseAsync()
        {
            EnsureProvider();

            var testId = 1;

            var testWeather = new Fixture().Build<Weather>().With(w => w.Id, testId).Create();

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            unitOfWorkMock
                .Setup(data => data.Weathers.DeleteAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            unitOfWorkMock
                .Setup(data => data.Weathers.SelectAsync(It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
                .Returns(Task.FromResult<Weather?>(testWeather));

            var faceTecMock = new Mock<IFaceTecApiManager>();
            var service = new ConsigBoilerplateService(unitOfWorkMock.Object, faceTecMock.Object);

            // Act
            var result = await service.DeleteWeatherAsync(testId, It.IsAny<CancellationToken>());

            // ShouldBe
            result.ShouldBeFalse();

            unitOfWorkMock.Verify();
        }

        [Fact]
        public async Task DeleteWeather_ReturnIdFalseAsync()
        {
            EnsureProvider();

            var testId = 1;

            var unitOfWorkMock = new Mock<IUnitOfWorkOracle>();
            unitOfWorkMock
                .Setup(data => data.Weathers.SelectAsync(It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
                .Returns(Task.FromResult<Weather?>(null));

            var faceTecMock = new Mock<IFaceTecApiManager>();
            var service = new ConsigBoilerplateService(unitOfWorkMock.Object, faceTecMock.Object);

            // Act
            var result = await service.DeleteWeatherAsync(testId, It.IsAny<CancellationToken>());

            // ShouldBe
            result.ShouldBeFalse();

            unitOfWorkMock.Verify();
        }
    }
}
