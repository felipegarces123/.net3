using Bmg.Kafka;
using Bmg.Kafka.Common.Settings;
using Bmg.ConsigBoilerplate.Domain;
using Bmg.ConsigBoilerplate.Domain.Adapters.Integrations.Queues.WeatherConsumerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Bmg.ConsigBoilerplate.Api
{
    [ExcludeFromCodeCoverage]
    public static class ConsigBoilerplateKafkaDependency
    {
        /// <summary>
        /// Registra os producers Kafka do serviço.
        /// Requer que os parâmetros de broker estejam carregados via AddBmgParametersBrokers().
        ///
        /// TODO: Ajuste os nomes de cluster e producers conforme sua configuração no CNFG.
        /// TODO: Remova este módulo se o serviço não utilizar Kafka.
        /// </summary>
        public static IServiceCollection AddConsigBoilerplateKafkaModule(this IServiceCollection services, IConfiguration configuration)
        {
            var bmgKafkaSettings = configuration.GetSection(nameof(BmgKafkaSettings)).Get<BmgKafkaSettings>()
                ?? throw new InvalidOperationException(
                    "Configurações do Kafka não encontradas. Verifique se AddBmgParametersBrokers() foi chamado antes deste módulo.");

            services.AddBmgKafka(configure =>
            {
                // TODO: Substitua pelo nome do cluster configurado no CNFG
                var clusterName = "ClusterName";

                var clusterSettings = bmgKafkaSettings.Clusters?.FirstOrDefault(s => s.Name.Equals(clusterName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException($"Cluster '{clusterName}' não encontrado nas configurações do Kafka.");

                configure.AddCluster<KafkaCluster>(clusterSettings.Name, configureCluster =>
                {
                    configureCluster.UseBootstrapServers(clusterSettings.BootstrapServers);

                    var username = clusterSettings.Username
                        ?? throw new InvalidOperationException($"Credencial 'Username' não encontrada para o cluster '{clusterName}'.");
                    var password = clusterSettings.Password
                        ?? throw new InvalidOperationException($"Credencial 'Password' não encontrada para o cluster '{clusterName}'.");

                    configureCluster.WithCredentials(username, password);

                    // TODO: Adicione ou remova producers conforme necessário
                    configureCluster.AddProducer<string, WeatherMessage>(KafkaCluster.ConsigBoilerplateProducer, producer =>
                    {
                        var producerName = nameof(KafkaCluster.ConsigBoilerplateProducer);

                        var producerSettings = clusterSettings.Producers?.FirstOrDefault(s => s.Name.Equals(producerName, StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException($"Producer '{producerName}' não encontrado nas configurações do cluster '{clusterName}'.");

                        producer.Configure(config =>
                        {
                            config.SetName(producerSettings.Name);
                            config.SetTopic(producerSettings.Topic);
                            config.SetIdempotenceEnabled();
                        });
                    });
                });
            });

            return services;
        }
    }
}
