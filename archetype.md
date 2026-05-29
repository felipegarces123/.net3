# .Net Application Archetype Guide - Bmg.ConsigBoilerplate (Enterprise Complete)

## Overview
Este archetype enterprise utiliza Clean Architecture + Hexagonal Architecture (Ports & Adapters), focado em escalabilidade, desacoplamento, observabilidade e suporte a integrações externas e mensageria.

---
## Architecture Diagram
Driving (API) -> Application -> Domain <- Driven (Database, Integrations, Messaging)

---
## Project Structure
/Adapters
  /Driving
    /Apis
      /Controllers
      /Dtos
      /Validators
  /Driven
    /Database
    /Integrations
/Core
  /Application
  /Domain
/Tests

---
## Layers

### 1. Controller Layer
```csharp
[ApiController]
[Route("api/[controller]")]
public class ConsigBoilerplateController : ControllerBase
{
    private readonly IConsigBoilerplateAppService _service;

    public ConsigBoilerplateController(IConsigBoilerplateAppService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Process([FromBody] WeatherRequest request)
    {
        var result = await _service.ProcessAsync(request);
        return Ok(result);
    }
}
```

---
### 2. DTO Layer
```csharp
public class WeatherRequest
{
    public string City { get; set; }
}

public class WeatherResponse
{
    public string Temperature { get; set; }
}
```

---
### 3. Validator Layer
```csharp
public class WeatherRequestValidator : AbstractValidator<WeatherRequest>
{
    public WeatherRequestValidator()
    {
        RuleFor(x => x.City).NotEmpty();
    }
}
```

---
### 4. Application Layer
```csharp
public interface IConsigBoilerplateAppService
{
    Task<WeatherResponse> ProcessAsync(WeatherRequest request);
}

public class ConsigBoilerplateAppService : IConsigBoilerplateAppService
{
    private readonly IWeatherRepository _repository;

    public ConsigBoilerplateAppService(IWeatherRepository repository)
    {
        _repository = repository;
    }

    public async Task<WeatherResponse> ProcessAsync(WeatherRequest request)
    {
        var model = await _repository.GetWeather(request.City);

        return new WeatherResponse
        {
            Temperature = model.Temperature
        };
    }
}
```

---
### 5. Domain Layer
```csharp
public class WeatherModel
{
    public string City { get; set; }
    public string Temperature { get; set; }
}

public interface IWeatherRepository
{
    Task<WeatherModel> GetWeather(string city);
}
```

---
### 6. Repository Layer
```csharp
public class WeatherRepository : IWeatherRepository
{
    public async Task<WeatherModel> GetWeather(string city)
    {
        return new WeatherModel
        {
            City = city,
            Temperature = "25°C"
        };
    }
}
```

---
### 7. Integration Layer
```csharp
public class MetabuscaApiManager : IMetabuscaApiManager
{
    public async Task<string> GetDataAsync(string document)
    {
        return "external-data";
    }
}
```

---
## Observability (Enterprise)

### Logging (Serilog)
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
```

### OpenTelemetry
```csharp
builder.Services.AddOpenTelemetryTracing();
```

---
## Messaging (Kafka Example)
```csharp
public class WeatherProducer
{
    public async Task SendAsync(string message)
    {
        // Envia mensagem para Kafka
    }
}

public class WeatherConsumer
{
    public async Task ConsumeAsync()
    {
        // Consome mensagem
    }
}
```

---
## Security
```csharp
builder.Services.AddAuthentication("Bearer");
```

---
## End-to-End Flow
1. Controller recebe request
2. Validator valida
3. AppService executa
4. Domain define regra
5. Repository ou Integration executa
6. Retorno ao Controller

---
## Best Practices
- SOLID
- Baixo acoplamento
- DTOs sempre
- Logs estruturados
- Observabilidade obrigatória

---
## Dev Guide
Criar feature:
1. Domain
2. Application
3. Driven
4. Driving
5. Tests

---
## CI/CD (Sugestão)
- Build (dotnet build)
- Test (dotnet test)
- Publish
- Deploy

---
## Objective
Template base para APIs enterprise escaláveis, resilientes e desacopladas.
