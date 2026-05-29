namespace Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Queues.WeatherConsumerService
{
    public class WeatherMessage
    {
        public string WeatherId { get; set; }
        public DateTime Date { get; set; }
    }
}
